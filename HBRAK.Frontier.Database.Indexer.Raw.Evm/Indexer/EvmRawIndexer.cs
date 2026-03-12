using HBRAK.Frontier.Database.Indexer.Raw.Context;
using HBRAK.Frontier.Database.Indexer.Raw.Evm.Data;
using HBRAK.Frontier.Database.Indexer.Raw.Evm.Helpers;
using HBRAK.Frontier.Database.Indexer.Raw.Objects;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Diagnostics;
using System.Threading.Channels;

namespace HBRAK.Frontier.Database.Indexer.Raw.Evm.Indexer;

public sealed class EvmRawIndexer : IRawIndexer
{
    private readonly ILogger _log;
    private readonly EvmRawIndexerOptions _settings;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly string _connString;

    private const long RunOnceWindowBlocks = 100_000;

    public EvmRawIndexer(IOptions<EvmRawIndexerOptions> options, ILogger<EvmRawIndexer> logger, IServiceScopeFactory scopeFactory)
    {
        _log = logger;
        _scopeFactory = scopeFactory;
        _settings = options.Value;
        if (string.IsNullOrWhiteSpace(_settings.RpcUrl))
            throw new InvalidOperationException("Indexing:RpcUrl must be set");
        _connString = $"Data Source={Environment.ExpandEnvironmentVariables(_settings.SqlitePath)}";
        
    }

    public async Task<bool> RunOnceAsync(CancellationToken ct)
    {


        // Resolve EF's connection string (same file for EF + indexer)
        string connString;
        await using (var scope = _scopeFactory.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<FrontierRawDb>();
            connString = db.Database.GetConnectionString()
                          ?? throw new InvalidOperationException("FrontierRawDb has no configured connection string.");
            var path = new SqliteConnectionStringBuilder(connString).DataSource;
            _log.LogInformation("SQLite DB (EF+Indexer): {Path}", Path.GetFullPath(path));
        }

        // Speed PRAGMAs
        await using (var con = new SqliteConnection(connString))
            await FastSqlite.ApplyPragmasAsync(con, _settings.Sqlite, ct);

        // RPC client + fetcher
        using var rpc = new RpcClient(_settings.RpcUrl);
        var fetcher = new AdaptiveLogFetcher(rpc, _settings);

        // Writer for Logs table
        var writer = await FastSqlite.CreateWriterAsync(connString, ct);

        // Sanity log (db path, table, cols, counts)
        await using (var con2 = new SqliteConnection(connString))
        {
            await con2.OpenAsync(ct);
            var path = Path.GetFullPath(new SqliteConnectionStringBuilder(connString).DataSource);
            _log.LogInformation("[WriterInit] DB: {Path}, Table: {Table}, Cols: {Cols}",
                path, writer.TableName, string.Join(",", writer.Columns));

            var cmd0 = con2.CreateCommand();
            cmd0.CommandText = $"SELECT COUNT(*), COALESCE(MIN(BlockNumber),-1), COALESCE(MAX(BlockNumber),-1) FROM {writer.TableName};";
            await using var rd0 = await cmd0.ExecuteReaderAsync(ct);
            if (await rd0.ReadAsync(ct))
                _log.LogInformation("[WriterInit] Existing => count={Count} min={Min} max={Max}",
                    rd0.GetInt64(0), rd0.GetInt64(1), rd0.GetInt64(2));
        }

        // Decide where to start
        var startFrom = await ResolveStartFromAsync(connString, ct);

        var head = await fetcher.GetHeadAsync(ct);
        var safeHead = Math.Max(0, head - _settings.Confirmations);
        if (startFrom > safeHead)
        {
            _log.LogInformation("Up-to-date. cursor={Cursor}, safeHead={SafeHead}", startFrom, safeHead);
            return false;
        }

        var windowEnd = Math.Min(safeHead, startFrom + RunOnceWindowBlocks - 1);
        _log.LogInformation("RunOnce window: [{Start}-{End}] (safeHead={SafeHead})", startFrom, windowEnd, safeHead);

        // Detect whether Logs.BlockTime is NOT NULL → require timestamp enrichment
        bool requireBlockTime;
        await using (var conCheck = new SqliteConnection(connString))
        {
            await conCheck.OpenAsync(ct);
            var cmd = conCheck.CreateCommand();
            cmd.CommandText = "PRAGMA table_info(Logs);";
            var nonNull = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
            await using var rd = await cmd.ExecuteReaderAsync(ct);
            while (await rd.ReadAsync(ct))
            {
                var name = rd.GetString(1);
                var notnull = rd.GetInt32(3) == 1;
                nonNull[name] = notnull;
            }
            requireBlockTime = nonNull.TryGetValue("BlockTime", out var nn) && nn;
        }

        // —— pipeline ——
        var ranges = Channel.CreateBounded<(long from, long to)>(new BoundedChannelOptions(16) { SingleWriter = true, SingleReader = false });
        var batches = Channel.CreateBounded<List<RawLogRow>>(new BoundedChannelOptions(16) { SingleWriter = false, SingleReader = false });

        // Producer
        var producer = Task.Run(async () =>
        {
            const long coarse = 10_000;
            for (long f = startFrom; f <= windowEnd; f += coarse + 1)
            {
                long t = Math.Min(windowEnd, f + coarse);
                await ranges.Writer.WriteAsync((f, t), ct);
            }
            ranges.Writer.Complete();
        }, ct);

        // Workers
        var workers = Enumerable.Range(0, _settings.DegreeOfParallelism).Select(_ => Task.Run(async () =>
        {
            while (await ranges.Reader.WaitToReadAsync(ct))
            {
                while (ranges.Reader.TryRead(out var r))
                {
                    var sw = Stopwatch.StartNew();
                    var wire = await fetcher.GetLogsRangeAdaptiveAsync(r.from, r.to, ct);
                    sw.Stop();

                    // Force enrichment if BlockTime is required
                    Dictionary<long, long>? tsMap = null;
                    bool enrichTs = requireBlockTime || _settings.EnrichWithBlockTimestamps;
                    if (enrichTs && wire.Count > 0)
                        tsMap = await fetcher.GetBlockTimestampsAsync(wire.Select(x => x.BlockNumber), ct);

                    var rows = new List<RawLogRow>(wire.Count);
                    foreach (var l in wire)
                    {
                        // topics as bytes
                        byte[] t0 = l.Topics.Length > 0 ? B(l.Topics[0]) : Array.Empty<byte>();
                        byte[]? t1 = l.Topics.Length > 1 ? B(l.Topics[1]) : null;
                        byte[]? t2 = l.Topics.Length > 2 ? B(l.Topics[2]) : null;
                        byte[]? t3 = l.Topics.Length > 3 ? B(l.Topics[3]) : null;

                        rows.Add(new RawLogRow(
                            BlockNumber: l.BlockNumber,
                            TxHash: B(l.TxHash),                // 32 bytes
                            LogIndex: l.LogIndex,
                            Address: B(l.Address),               // 20 bytes (RPC returns 20B)
                            Topic0: t0,
                            Topic1: t1,
                            Topic2: t2,
                            Topic3: t3,
                            Data: string.IsNullOrEmpty(l.DataHex) ? null : B(l.DataHex),
                            BlockTime: tsMap != null && tsMap.TryGetValue(l.BlockNumber, out var ts) ? ts : 0
                        ));
                    }

                    foreach (var chunk in Chunk(rows, _settings.BatchInsertSize))
                        await batches.Writer.WriteAsync(chunk, ct);

                    _log.LogInformation("Fetched {N} logs for [{F}-{T}] in {Ms} ms", wire.Count, r.from, r.to, sw.ElapsedMilliseconds);
                }
            }
        }, ct)).ToArray();

        // Writer + cursor progress
        long highestCommitted = startFrom - 1;

        var writerTask = Task.Run(async () =>
        {
            await foreach (var batch in batches.Reader.ReadAllAsync(ct))
            {
                var inserted = await writer.InsertAsync(batch, ct);
                _log.LogInformation("Logs batch processed: {Processed} rows, {Inserted} inserted",
                    batch.Count, inserted);

                var maxBlock = batch.Count > 0 ? batch.Max(r => r.BlockNumber) : highestCommitted;
                if (maxBlock > highestCommitted)
                {
                    highestCommitted = maxBlock;
                    var safeCursor = Math.Max(0, highestCommitted - _settings.Confirmations);
                    await SaveCursorAsync(safeCursor, ct);
                }
            }
        }, ct);

        await producer;
        await Task.WhenAll(workers);
        batches.Writer.Complete();
        await writerTask;

        // fold WAL for visibility while verifying (remove later for perf)
        await using (var con3 = new SqliteConnection(connString))
        {
            await con3.OpenAsync(ct);
            await new SqliteCommand("PRAGMA wal_checkpoint(TRUNCATE);", con3).ExecuteNonQueryAsync(ct);
        }

        _log.LogInformation("RunOnce done. newCursor≈{Cursor}", await LoadCursorAsync(ct));
        return true;
    }

    static byte[] B(string hex) => Hex.HexToBytes(hex);

    // —— EF helpers via DI scopes ——

    private async Task<long> ResolveStartFromAsync(string connString, CancellationToken ct)
    {
        // prefer EF Cursor table
        await using (var scope = _scopeFactory.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<FrontierRawDb>();
            var cur = await db.Cursor.FirstOrDefaultAsync(ct);
            if (cur is not null && cur.LastProcessedBlock > 0) // adjust prop name if needed
                return cur.LastProcessedBlock;
        }

        // else resume after last Logs block
        await using var con = new SqliteConnection(connString);
        await con.OpenAsync(ct);
        var cmd = con.CreateCommand();
        cmd.CommandText = "SELECT COALESCE(MAX(BlockNumber)+1, 0) FROM InputLogs;";
        var val = await cmd.ExecuteScalarAsync(ct);
        return Convert.ToInt64(val);
    }

    private async Task SaveCursorAsync(long block, CancellationToken ct)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<FrontierRawDb>();

        var cur = await db.Cursor.FirstOrDefaultAsync(ct);
        if (cur is null)
        {
            db.Cursor.Add(new RawCursor { Id = 1, LastProcessedBlock = block }); // adjust if different
        }
        else
        {
            if (block > cur.LastProcessedBlock) cur.LastProcessedBlock = block;
        }
        await db.SaveChangesAsync(ct);
    }

    private async Task<long> LoadCursorAsync(CancellationToken ct)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<FrontierRawDb>();
        var cur = await db.Cursor.FirstOrDefaultAsync(ct);
        return cur?.LastProcessedBlock ?? 0; // adjust if different
    }

    private static IEnumerable<List<T>> Chunk<T>(List<T> src, int size)
    {
        for (int i = 0; i < src.Count; i += size)
            yield return src.GetRange(i, Math.Min(size, src.Count - i));
    }
}