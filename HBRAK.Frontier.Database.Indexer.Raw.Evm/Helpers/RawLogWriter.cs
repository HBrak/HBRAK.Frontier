using HBRAK.Frontier.Database.Indexer.Raw.Evm.Data;
using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HBRAK.Frontier.Database.Indexer.Raw.Evm.Helpers;

internal sealed class RawLogWriter
{
    private readonly string _conn;
    private readonly string _table;
    private readonly string[] _cols;
    private readonly FastSqlite.InsertMap _map;
    private readonly bool _hasTopicsJson;
    private readonly bool _hasData;

    private bool _loggedSample;

    public RawLogWriter(string conn, string table, string[] cols,
        FastSqlite.InsertMap map, bool hasTopicsJson, bool hasData)
    {
        _conn = conn; _table = table; _cols = cols; _map = map; _hasTopicsJson = hasTopicsJson; _hasData = hasData;
    }

    public string TableName => _table;
    public IReadOnlyList<string> Columns => _cols;

    /// <summary>Insert a batch; returns how many rows were actually inserted.</summary>
    public async Task<long> InsertAsync(IReadOnlyList<RawLogRow> rows, CancellationToken ct)
    {
        if (rows.Count == 0) return 0;

        await using var con = new SqliteConnection(_conn);
        await con.OpenAsync(ct);
        await using var tx = con.BeginTransaction();

        var columnsCsv = string.Join(",", _cols);
        var placeholdersCsv = string.Join(",", _cols.Select(c => $"${c}"));

        var cmd = con.CreateCommand();
        cmd.Transaction = tx;
        cmd.CommandText =
            $"INSERT INTO {_table} ({columnsCsv}) VALUES ({placeholdersCsv}) " +
            "ON CONFLICT(TxHash, LogIndex) DO NOTHING;";

        var p = _cols.ToDictionary(c => c, c => cmd.Parameters.Add($"${c}", GuessType(c)), StringComparer.OrdinalIgnoreCase);

        long inserted = 0;

        for (int idx = 0; idx < rows.Count; idx++)
        {
            var r = rows[idx];

            p["TxHash"].Value = r.TxHash;
            p["LogIndex"].Value = r.LogIndex;
            p["Address"].Value = r.Address;

            p["Topic0"].Value = r.Topic0;
            if (_map.HasTopic1 && p.TryGetValue("Topic1", out var t1)) t1.Value = (object?)r.Topic1 ?? DBNull.Value;
            if (_map.HasTopic2 && p.TryGetValue("Topic2", out var t2)) t2.Value = (object?)r.Topic2 ?? DBNull.Value;
            if (_map.HasTopic3 && p.TryGetValue("Topic3", out var t3)) t3.Value = (object?)r.Topic3 ?? DBNull.Value;

            p["BlockNumber"].Value = r.BlockNumber;
            p["BlockTime"].Value = r.BlockTime;

            if (_hasData && p.TryGetValue("Data", out var pd)) pd.Value = (object?)r.Data ?? DBNull.Value;

            if (!_loggedSample)
            {
                _loggedSample = true;
                Console.WriteLine($"[RawLogWriter] Sample -> TxHash={r.TxHash}, LogIndex={r.LogIndex}, Block={r.BlockNumber}, Addr={r.Address}, Ts={r.BlockTime}");
            }

            var affected = await cmd.ExecuteNonQueryAsync(ct);
            if (affected > 0) inserted += affected;
        }

        await tx.CommitAsync(ct);

        // Make growth visible while verifying (remove later for perf)
        await using (var chk = new SqliteConnection(_conn))
        {
            await chk.OpenAsync(ct);
            await new SqliteCommand("PRAGMA wal_checkpoint(TRUNCATE);", chk).ExecuteNonQueryAsync(ct);
        }

        return inserted;
    }

    private static SqliteType GuessType(string column) => column switch
    {
        "BlockNumber" or "LogIndex" or "BlockTime" => SqliteType.Integer,
        "TxHash" or "Address" or "Topic0" or "Topic1" or "Topic2" or "Topic3" or "Data" => SqliteType.Blob,
        _ => SqliteType.Text
    };
}