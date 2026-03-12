using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace HBRAK.Frontier.Database.Indexer.Raw.Evm;

internal sealed class RpcClient : IDisposable
{
    private static readonly SocketsHttpHandler Handler = new()
    {
        PooledConnectionLifetime = TimeSpan.FromMinutes(5),
        PooledConnectionIdleTimeout = TimeSpan.FromMinutes(2),
        KeepAlivePingDelay = TimeSpan.FromSeconds(30),
        KeepAlivePingTimeout = TimeSpan.FromSeconds(10),
        AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate,
        EnableMultipleHttp2Connections = true
    };

    private readonly HttpClient _http;
    private readonly JsonSerializerOptions _json = new(JsonSerializerDefaults.Web);
    private readonly Uri _uri;

    public RpcClient(string rpcUrl)
    {
        _http = new HttpClient(Handler, disposeHandler: false) { Timeout = TimeSpan.FromSeconds(60) };
        _uri = new Uri(rpcUrl, UriKind.Absolute);
    }

    public async Task<JsonElement> CallAsync(string method, object? @params, CancellationToken ct)
    {
        var payload = new { jsonrpc = "2.0", id = Random.Shared.Next(), method, @params };
        using var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
        using var resp = await _http.PostAsync(_uri, content, ct).ConfigureAwait(false);
        resp.EnsureSuccessStatusCode();

        var bytes = await resp.Content.ReadAsByteArrayAsync(ct).ConfigureAwait(false);
        using var doc = JsonDocument.Parse(bytes);

        if (doc.RootElement.TryGetProperty("error", out var err))
            throw new InvalidOperationException($"RPC error {err.GetProperty("code").GetInt32()}: {err.GetProperty("message").GetString()}");

        // IMPORTANT: Clone so the returned JsonElement is not tied to the disposed document
        return doc.RootElement.GetProperty("result").Clone();
    }

    public async Task<JsonElement[]> CallBatchAsync((string method, object? @params)[] calls, CancellationToken ct)
    {
        if (calls == null || calls.Length == 0)
            return Array.Empty<JsonElement>();

        var results = new JsonElement[calls.Length];
        var cap = GetCap();
        int nextId = 1;

        for (int i = 0; i < calls.Length;)
        {
            // start with current tuned caps
            int take = Math.Min(cap.MaxItems, calls.Length - i);

            while (true)
            {
                if (take <= 0) take = 1;

                // build a sub-batch and check byte size; if too big, shrink before POST
                var (jsonBytes, idToIndex) = BuildSubBatch(calls, i, take, ref nextId);

                // enforce byte cap by shrinking in half until under limit (or down to 1)
                while (jsonBytes.Length > cap.MaxBytes && take > 1)
                {
                    take = Math.Max(1, take / 2);
                    nextId -= idToIndex.Count; // rewind ids we generated
                    (jsonBytes, idToIndex) = BuildSubBatch(calls, i, take, ref nextId);
                }

                try
                {
                    using var content = new ByteArrayContent(jsonBytes);
                    content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");

                    using var resp = await _http.PostAsync(_uri, content, ct).ConfigureAwait(false);

                    // Payload too large at HTTP level → shrink & retry
                    if (resp.StatusCode == (HttpStatusCode)413 /*Payload Too Large*/)
                    {
                        Shrink(cap);
                        take = Math.Max(1, take / 2);
                        continue; // retry this segment smaller
                    }

                    resp.EnsureSuccessStatusCode();

                    var bytes = await resp.Content.ReadAsByteArrayAsync(ct).ConfigureAwait(false);
                    using var doc = JsonDocument.Parse(bytes);

                    if (doc.RootElement.ValueKind != JsonValueKind.Array)
                        throw new InvalidOperationException("RPC batch response is not an array.");

                    bool providerSaysTooLarge = false;

                    foreach (var el in doc.RootElement.EnumerateArray())
                    {
                        if (el.TryGetProperty("error", out var err))
                        {
                            if (IsBatchTooLargeError(err))
                            {
                                providerSaysTooLarge = true;
                                break;
                            }

                            var code = err.GetProperty("code").GetInt32();
                            var msg = err.GetProperty("message").GetString();
                            throw new InvalidOperationException($"RPC batch error {code}: {msg}");
                        }

                        var id = el.GetProperty("id").GetInt32();
                        if (!idToIndex.TryGetValue(id, out var idx))
                            continue;

                        // clone so it outlives the JsonDocument
                        results[idx] = el.GetProperty("result").Clone();
                    }

                    if (providerSaysTooLarge)
                    {
                        // shrink caps and retry this same segment
                        Shrink(cap);
                        take = Math.Max(1, take / 2);
                        // rewind ids for this segment
                        nextId -= idToIndex.Count;
                        continue;
                    }

                    // success → optionally grow caps a bit
                    Grow(cap, take, jsonBytes.Length);

                    i += take;
                    break; // proceed to next segment
                }
                catch (TaskCanceledException) when (!ct.IsCancellationRequested)
                {
                    // request timed out – back off slightly and retry this segment
                    Shrink(cap);
                    take = Math.Max(1, take / 2);
                    // rewind ids for this segment
                    nextId -= idToIndex.Count;
                    continue;
                }
                catch (HttpRequestException httpEx) when (httpEx.StatusCode == HttpStatusCode.RequestEntityTooLarge)
                {
                    // 413 via exception path
                    Shrink(cap);
                    take = Math.Max(1, take / 2);
                    continue;
                }
                catch (InvalidOperationException ex) when (ex.Message.IndexOf("batch too large", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    Shrink(cap);
                    take = Math.Max(1, take / 2);
                    continue;
                }
                catch
                {
                    // Unknown failure: if we're already at single-call, fall back to CallAsync per item
                    if (take == 1)
                    {
                        var single = calls[i];
                        results[i] = await CallAsync(single.method, single.@params, ct);
                        i += 1;
                        break;
                    }
                    else
                    {
                        // shrink and retry this segment
                        Shrink(cap);
                        take = Math.Max(1, take / 2);
                        continue;
                    }
                }
            }
        }

        return results;
    }

    public void Dispose() => _http.Dispose();

    private sealed class BatchCap
    {
        public int MaxItems;
        public int MaxBytes;
        public DateTime LastTunedUtc;
    }

    private static readonly ConcurrentDictionary<string, BatchCap> _caps = new();

    private BatchCap GetCap()
    {
        // sensible defaults; will auto-tune
        return _caps.GetOrAdd(_uri.ToString(), _ => new BatchCap
        {
            MaxItems = 60,          // initial target calls per batch
            MaxBytes = 512 * 1024,  // ~512 KB JSON payload
            LastTunedUtc = DateTime.UtcNow
        });
    }

    private static void Shrink(BatchCap cap)
    {
        lock (cap)
        {
            cap.MaxItems = Math.Max(5, (int)(cap.MaxItems * 0.6));
            cap.MaxBytes = Math.Max(64 * 1024, (int)(cap.MaxBytes * 0.6));
            cap.LastTunedUtc = DateTime.UtcNow;
        }
    }

    private static void Grow(BatchCap cap, int usedItems, int usedBytes)
    {
        lock (cap)
        {
            // if we used the full item window and stayed comfortably under byte limit → grow items
            if (usedItems >= cap.MaxItems && usedBytes < (int)(cap.MaxBytes * 0.75))
                cap.MaxItems = Math.Min(200, cap.MaxItems + Math.Max(1, cap.MaxItems / 5));

            // if bytes are well below cap → grow bytes a bit
            if (usedBytes < (int)(cap.MaxBytes * 0.50))
                cap.MaxBytes = Math.Min(2_000_000, cap.MaxBytes + Math.Max(32 * 1024, cap.MaxBytes / 6));

            cap.LastTunedUtc = DateTime.UtcNow;
        }
    }

    private static bool IsBatchTooLargeError(JsonElement err)
    {
        var code = err.TryGetProperty("code", out var cEl) ? cEl.GetInt32() : 0;
        var msg = err.TryGetProperty("message", out var mEl) ? mEl.GetString() ?? "" : "";

        // common provider signals
        if (code == -32600 || code == -32000 || code == -32005) return true;
        if (msg.IndexOf("batch", StringComparison.OrdinalIgnoreCase) >= 0 &&
            (msg.IndexOf("large", StringComparison.OrdinalIgnoreCase) >= 0 ||
             msg.IndexOf("limit", StringComparison.OrdinalIgnoreCase) >= 0))
            return true;

        return false;
    }

    private static (byte[] jsonBytes, Dictionary<int, int> idToIndex) BuildSubBatch(
    (string method, object? @params)[] calls, int start, int take, ref int nextId)
    {
        var payload = new List<object>(take);
        var idToIndex = new Dictionary<int, int>(take);

        for (int j = 0; j < take; j++)
        {
            int id = nextId++;
            idToIndex[id] = start + j;

            var c = calls[start + j];
            payload.Add(new
            {
                jsonrpc = "2.0",
                id,
                method = c.method,
                @params = c.@params
            });
        }

        // serialize to UTF-8 bytes (we’ll check size before POST)
        var jsonBytes = JsonSerializer.SerializeToUtf8Bytes(payload);
        return (jsonBytes, idToIndex);
    }
}
