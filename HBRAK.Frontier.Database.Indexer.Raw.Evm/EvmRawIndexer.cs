using HBRAK.Frontier.Database.Indexer.Raw.Context;
using Microsoft.Extensions.Logging;
using Nethereum.RPC.Eth.DTOs;
using Nethereum.Web3;
using Microsoft.Extensions.Options;
using HBRAK.Frontier.Database.Indexer.Raw.Objects;
using Nethereum.Hex.HexConvertors.Extensions;

namespace HBRAK.Frontier.Database.Indexer.Raw.Evm;

public sealed class EvmRawIndexer : IRawIndexer
{
    private readonly FrontierRawDb _db;
    private readonly ILogger<EvmRawIndexer> _log;
    private readonly EvmRawIndexerOptions _opt;
    private readonly Web3 _web3;

    public EvmRawIndexer(FrontierRawDb db, IOptions<EvmRawIndexerOptions> opt, ILogger<EvmRawIndexer> log)
    {
        _db = db;
        _opt = opt.Value;
        _log = log;
        _web3 = new Web3(_opt.RpcUrl);
    }

    public async Task<long> GetLastProcessedAsync(CancellationToken ct = default)
    {
        var c = await _db.Cursor.FindAsync([1], ct);
        return c?.LastProcessedBlock ?? (_opt.StartBlock > 0 ? _opt.StartBlock - 1 : -1);
    }

    public async Task<bool> RunOnceAsync(CancellationToken ct = default)
    {
        await EnsureCursorMatchesOptionsAsync(ct);

        // 1) determine range
        var latest = (long)(await _web3.Eth.Blocks.GetBlockNumber.SendRequestAsync()).Value;
        var safeTip = latest - _opt.Confirmations;
        if (safeTip < 0) return false;

        var cursor = await _db.Cursor.FindAsync([1], ct) ?? new RawCursor { Id = 1, LastProcessedBlock = _opt.StartBlock - 1 };
        var from = Math.Max(cursor.LastProcessedBlock + 1, _opt.StartBlock);
        if (from > safeTip) return false;

        var to = Math.Min(from + _opt.BatchSize - 1, safeTip);

        _log.LogInformation("Ingesting blocks {From}..{To} (latest {Latest}, safe {Safe})", from, to, latest, safeTip);

        // 2) fetch logs in the range (filter by addresses if provided)
        var filter = new NewFilterInput
        {
            FromBlock = new BlockParameter(new Nethereum.Hex.HexTypes.HexBigInteger(from)),
            ToBlock = new BlockParameter(new Nethereum.Hex.HexTypes.HexBigInteger(to)),
            Address = _opt.Addresses is { Length: > 0 } ? _opt.Addresses : null
        };

        var logs = await _web3.Eth.Filters.GetLogs.SendRequestAsync(filter);

        // 3) collect involved block numbers & tx hashes (to fetch blocks/txs)
        var blockNums = logs.Select(l => (long)l.BlockNumber.Value).Distinct().OrderBy(x => x).ToList();
        var txHashes = logs.Select(l => l.TransactionHash).Distinct().ToList();

        // 4) upsert blocks (with timestamps) and transactions (minimal)
        foreach (var bn in blockNums)
        {
            var b = await _web3.Eth.Blocks.GetBlockWithTransactionsByNumber
                                          .SendRequestAsync(new BlockParameter(new Nethereum.Hex.HexTypes.HexBigInteger(bn)));

            if (b is null) continue;

            await UpsertBlockAsync(bn, b);
            foreach (var tx in b.Transactions ?? [])
            {
                if (!txHashes.Contains(tx.TransactionHash)) continue; // only those that emitted our logs
                await UpsertTxAsync(tx, bn);
            }
            await _db.SaveChangesAsync(ct);
        }

        // 5) write log rows
        foreach (var l in logs)
        {
            var time = await GetBlockTimeAsync((long)l.BlockNumber.Value, ct); // from the block we just stored
            await UpsertLogAsync(l, time);
        }
        await _db.SaveChangesAsync(ct);

        // 6) advance cursor
        cursor.LastProcessedBlock = to;
        _db.Update(cursor);
        await _db.SaveChangesAsync(ct);

        return true;
    }

    private async Task<DateTimeOffset> GetBlockTimeAsync(long number, CancellationToken ct)
    {
        var row = await _db.Blocks.FindAsync([number], ct);
        return row?.Timestamp ?? DateTimeOffset.UnixEpoch;
    }

    private Task UpsertBlockAsync(long number, BlockWithTransactions b)
    {
        var row = _db.Blocks.Local.FirstOrDefault(x => x.Number == number) ?? _db.Blocks.Find(number);
        if (row is null)
        {
            _db.Blocks.Add(new BlockRow
            {
                Number = number,
                Hash = b.BlockHash,
                ParentHash = b.ParentHash,
                Timestamp = DateTimeOffset.FromUnixTimeSeconds((long)b.Timestamp.Value)
            });
        }
        else
        {
            row.Hash = b.BlockHash;
            row.ParentHash = b.ParentHash;
            row.Timestamp = DateTimeOffset.FromUnixTimeSeconds((long)b.Timestamp.Value);
        }
        return Task.CompletedTask;
    }

    private Task UpsertTxAsync(Transaction tx, long blockNumber)
    {
        var row = _db.Txs.Local.FirstOrDefault(x => x.Hash == tx.TransactionHash) ?? _db.Txs.Find(tx.TransactionHash);
        if (row is null)
        {
            _db.Txs.Add(new TxRow
            {
                Hash = tx.TransactionHash,
                BlockNumber = blockNumber,
                IndexInBlock = (int)tx.TransactionIndex.Value,
                From = tx.From,
                To = tx.To,
                InputHex = tx.Input
            });
        }
        else
        {
            row.BlockNumber = blockNumber;
            row.IndexInBlock = (int)tx.TransactionIndex.Value;
            row.From = tx.From;
            row.To = tx.To;
            row.InputHex = tx.Input;
        }
        return Task.CompletedTask;
    }


    private Task UpsertLogAsync(FilterLog l, DateTimeOffset blockTime)
    {
        var topics = (l.Topics ?? [])
            .Select(t => t switch
            {
                null => "0x",
                string s => s,
                _ => t.ToString() ?? "0x"
            })
            .ToArray();

        var topic0 = topics.Length > 0 ? topics[0] : "0x";
        var dataBytes = (l.Data is string ds && ds.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            ? ds.HexToByteArray()
            : [];

        var key = new object[] { l.TransactionHash!, (int)l.LogIndex!.Value };

        var row = _db.Logs.Local.FirstOrDefault(x => x.TxHash == l.TransactionHash && x.LogIndex == (int)l.LogIndex!.Value)
               ?? _db.Logs.Find(key);

        if (row is null)
        {
            _db.Logs.Add(new LogRow
            {
                TxHash = l.TransactionHash!,
                LogIndex = (int)l.LogIndex!.Value,
                Address = l.Address!,
                Topic0 = topic0,
                Topics = topics,
                Data = dataBytes,
                BlockNumber = (long)l.BlockNumber!.Value,
                BlockTime = blockTime
            });
        }
        else
        {
            row.Address = l.Address!;
            row.Topic0 = topic0;
            row.Topics = topics;
            row.Data = dataBytes;
            row.BlockNumber = (long)l.BlockNumber!.Value;
            row.BlockTime = blockTime;
        }

        return Task.CompletedTask;
    }

    private async Task EnsureCursorMatchesOptionsAsync(CancellationToken ct)
    {
        var cur = await _db.Cursor.FindAsync([1], ct);
        if (cur is null)
        {
            // first run → seed cursor with options ChainId
            cur = new RawCursor { Id = 1, ChainId = _opt.ChainId, LastProcessedBlock = _opt.StartBlock - 1 };
            _db.Cursor.Add(cur);
            await _db.SaveChangesAsync(ct);
            return;
        }

        if (cur.ChainId != _opt.ChainId)
            throw new InvalidOperationException($"ChainId mismatch: DB={cur.ChainId}, Options={_opt.ChainId}.");
    }

}

