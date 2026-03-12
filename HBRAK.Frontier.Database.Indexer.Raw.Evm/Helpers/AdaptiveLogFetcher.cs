using HBRAK.Frontier.Database.Indexer.Raw.Evm.Indexer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace HBRAK.Frontier.Database.Indexer.Raw.Evm.Helpers;

internal sealed class AdaptiveLogFetcher
{
    private readonly RpcClient _rpc;
    private readonly EvmRawIndexerOptions _cfg;

    public AdaptiveLogFetcher(RpcClient rpc, EvmRawIndexerOptions cfg) { _rpc = rpc; _cfg = cfg; }

    public async Task<long> GetHeadAsync(CancellationToken ct)
    {
        var headHex = (await _rpc.CallAsync("eth_blockNumber", Array.Empty<object>(), ct)).GetString()!;
        return (long)ToULong(headHex);
    }

    public async Task<Dictionary<long, long>> GetBlockTimestampsAsync(IEnumerable<long> blockNumbers, CancellationToken ct)
    {
        var nums = blockNumbers.Distinct().ToArray();
        if (nums.Length == 0) return new();
        var calls = nums
            .Select(n => (method: "eth_getBlockByNumber",
                          @params: (object?)new object[] { $"0x{n:X}", false }))
            .ToArray();

        var res = await _rpc.CallBatchAsync(calls, ct);

        var dict = new Dictionary<long, long>(nums.Length);
        for (int i = 0; i < nums.Length; i++)
            dict[nums[i]] = (long)ToULong(res[i].GetProperty("timestamp").GetString()!);

        return dict;
    }

    public async Task<List<WireLog>> GetLogsRangeAdaptiveAsync(long from, long to, CancellationToken ct)
    {
        long span = Math.Clamp(_cfg.InitialRangeSpan, _cfg.MinRangeSpan, _cfg.MaxRangeSpan);
        var list = new List<WireLog>(4096);
        long start = from;

        while (start <= to)
        {
            long end = Math.Min(to, start + span);
            var filter = BuildFilter(start, end);

            JsonElement result;
            var sw = System.Diagnostics.Stopwatch.StartNew();
            try { result = await _rpc.CallAsync("eth_getLogs", new[] { filter }, ct); }
            catch { span = Math.Max(_cfg.MinRangeSpan, span / 2); continue; }
            sw.Stop();

            foreach (var el in result.EnumerateArray())
            {
                list.Add(new WireLog(
                    BlockNumber: (long)ToULong(el.GetProperty("blockNumber").GetString()!),
                    LogIndex: (int)ToULong(el.GetProperty("logIndex").GetString()!),
                    TxHash: el.GetProperty("transactionHash").GetString()!,
                    Address: el.GetProperty("address").GetString()!,
                    Topics: el.GetProperty("topics").EnumerateArray().Select(t => t.GetString()!).ToArray(),
                    DataHex: el.GetProperty("data").GetString()!
                ));
            }

            if (sw.ElapsedMilliseconds > 2000 || list.Count > 10_000)
                span = Math.Max(_cfg.MinRangeSpan, span / 2);
            else if (sw.ElapsedMilliseconds < 600)
                span = Math.Min(_cfg.MaxRangeSpan, span * 2);

            start = end + 1;
        }

        return list;
    }

    private object BuildFilter(long from, long to)
    {
        var f = new Dictionary<string, object?>
        {
            ["fromBlock"] = $"0x{from:X}",
            ["toBlock"] = $"0x{to:X}",
        };
        if (!string.IsNullOrWhiteSpace(_cfg.FilterAddress)) f["address"] = _cfg.FilterAddress;
        if (!string.IsNullOrWhiteSpace(_cfg.FilterTopic0)) f["topics"] = new object?[] { _cfg.FilterTopic0, null, null, null };
        return f;
    }

    private static ulong ToULong(string hex)
    { if (hex.StartsWith("0x", StringComparison.OrdinalIgnoreCase)) hex = hex[2..]; return hex.Length == 0 ? 0 : ulong.Parse(hex, System.Globalization.NumberStyles.AllowHexSpecifier); }
}

internal readonly record struct WireLog(long BlockNumber, int LogIndex, string TxHash, string Address, string[] Topics, string DataHex);
