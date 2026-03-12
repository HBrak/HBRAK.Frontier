using HBRAK.Frontier.Communication.Api.Data.Meta.AbisConfig;
using HBRAK.Frontier.Communication.Api.Service;
using HBRAK.Frontier.Database.Indexer.Raw.Context;
using HBRAK.Frontier.Database.Indexer.Raw.Decode;
using HBRAK.Frontier.Database.Indexer.Raw.Objects;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Nethereum.Util;
using System.Numerics;
using System.Text;
using static HBRAK.Frontier.Database.Indexer.Raw.Decode.MudStuff;

namespace HBRAK.Frontier.Database.Indexer.Raw;

public sealed class RawIngestHostedService(IRawIndexer indexer, IApiService api, IServiceScopeFactory scopes, ILogger<RawIngestHostedService> log)
    : BackgroundService
{
    private IReadOnlyDictionary<string, string> _resourceLabelMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    private AbiDecoderIndex _abiIndex = null!;
    private Dictionary<string, MudTableSchema> _schema = new(StringComparer.OrdinalIgnoreCase);
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Build helpers once
        var abiCfg = await api.GetAbisConfigAsync();
        _resourceLabelMap = ResourceIdUtils.BuildLabelMap(abiCfg);
        _abiIndex = AbiDecoderIndex.BuildFrom(abiCfg);
        _schema = MudSchemaRegistry.Build(abiCfg);

        await DecodeSampleBatchAsync(stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            var didWork = await indexer.RunOnceAsync(stoppingToken);
            if (!didWork)
                await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);

            // After each run: try decoding 100 random logs from last 50k
             await DecodeSampleBatchAsync(stoppingToken);
        }
    }

    private async Task DecodeSampleBatchAsync(CancellationToken ct)
    {
        const int RecentWindow = 50_000;
        const int SampleSize = 100;

        var sample = await GetRandomRecentLogsAsync(scopes, RecentWindow, SampleSize, ct);
        if (sample.Count == 0)
        {
            log.LogInformation("No rows found in InputLogs yet; skipping sample decode.");
            return;
        }

        int mudDecoded = 0, abiDecoded = 0, printed = 0;

        foreach (var row in sample)
        {
            // 1) Try MUD store events (names + humanized params)
            var mud = MudStoreDecoder.TryDecode(row);
            if (mud is not null)
            {
                mudDecoded++;
                LogMudHuman(mud);
            }

            // 2) Fallback to ABI event decode (names + humanized args)
            var ev = _abiIndex.TryDecode(row);
            if (ev is not null)
            {
                abiDecoded++;
                LogAbiHuman(ev);
            }
        }

        log.LogInformation(
            "Human decode sample: MUD={Mud}/{Total}, ABI={Abi}/{Total} (from last {Recent} rows).",
            mudDecoded, sample.Count, abiDecoded, sample.Count, RecentWindow);
    }

    private static async Task<List<InputLogRow>> GetRandomRecentLogsAsync(
        IServiceScopeFactory scopes, int recentCount, int sampleCount, CancellationToken ct)
    {
        await using var scope = scopes.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<FrontierRawDb>();

        var recent = db.InputLogs
            .AsNoTracking()
            .OrderByDescending(l => l.BlockNumber).ThenByDescending(l => l.LogIndex)
            .Take(recentCount);

        return await recent.OrderBy(_ => EF.Functions.Random()).Take(sampleCount).ToListAsync(ct);
    }

    // ----------------- Humanized logging -----------------

    private void LogMudHuman(MudStoreEvent ev)
    {
        _schema.TryGetValue(ev.TableIdHex, out var schema);
        var line = MudEventTranslator.ToHuman(ev, schema);
        log.LogInformation(line + "  (block={Block} idx={Idx})", ev.BlockNumber, ev.LogIndex);
    }

    private void LogAbiHuman(DecodedLog ev)
    {
        // EventName + key=value pairs, humanized; hide long hex
        var kvs = ev.Args.Select(kv => $"{kv.Key}={HumanArg(kv.Value)}");
        log.LogInformation("ABI.{Event}({Args})  block={Block} idx={Idx}",
            ev.EventName, string.Join(", ", kvs), ev.BlockNumber, ev.LogIndex);
    }
    private static string HumanArg(object? v)
    {
        if (v is null) return "null";

        // Nethereum pattern for indexed dynamic: { "__indexed_hash__": "0x..." }
        if (v is Dictionary<string, object?> d && d.TryGetValue("__indexed_hash__", out var _))
            return "indexed(dynamic)";

        if (v is bool b) return b ? "true" : "false";

        if (v is string s)
        {
            // decimal number? keep it
            if (s.Length > 0 && char.IsDigit(s[0])) return s;

            // hex-like?
            if (s.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            {
                if (s.Length == 42) // address
                    return AddressUtil.Current.ConvertToChecksumAddress(s);

                var data = HexToBytes(s);
                // try ASCII preview
                int printable = data.Count(ch => ch >= 32 && ch <= 126);
                if (printable >= Math.Min(16, data.Length))
                {
                    var txt = new string(data.Select(ch => (ch >= 32 && ch <= 126) ? (char)ch : '·').ToArray());
                    if (txt.Length > 64) txt = txt[..64] + "…";
                    return $"ascii(\"{txt}\")";
                }
                return $"bytes({data.Length})";
            }

            return s; // regular string
        }

        // BigInteger or numeric
        if (v is BigInteger bi) return bi.ToString();
        if (v is IFormattable fmt) return fmt.ToString(null, null);

        // Fallback
        return v.ToString() ?? "<?>";
    }

    private static byte[] HexToBytes(string hex)
    {
        if (string.IsNullOrEmpty(hex)) return Array.Empty<byte>();
        if (hex.StartsWith("0x", StringComparison.OrdinalIgnoreCase)) hex = hex[2..];
        int len = hex.Length / 2;
        var data = new byte[len];
        for (int i = 0; i < len; i++)
            data[i] = Convert.ToByte(hex.Substring(i * 2, 2), 16);
        return data;
    }
}