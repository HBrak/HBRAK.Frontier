using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HBRAK.Frontier.Database.Indexer.Raw.Evm.Helpers;

internal static class FastSqlite
{
    public static async Task ApplyPragmasAsync(SqliteConnection con, SqliteTuning t, CancellationToken ct)
    {
        if (con.State != System.Data.ConnectionState.Open) await con.OpenAsync(ct);
        var cmds = new[]
        {
            t.Wal ? "PRAGMA journal_mode=WAL;" : "PRAGMA journal_mode=DELETE;",
            $"PRAGMA synchronous={t.Synchronous};",
            "PRAGMA foreign_keys=ON;",
            "PRAGMA temp_store=MEMORY;",
            $"PRAGMA cache_size={t.CacheSizePages};",
            $"PRAGMA mmap_size={t.MMapSizeBytes};"
        };
        foreach (var sql in cmds)
            await new SqliteCommand(sql, con).ExecuteNonQueryAsync(ct);
    }

    /// <summary>
    /// Bind writer to your **Logs** table. Fails loudly if required columns are missing.
    /// Detects optional columns (Topic0..3, Topics, Data, Timestamp, BlockTime).
    /// </summary>
    public static async Task<RawLogWriter> CreateWriterAsync(string conn, CancellationToken ct)
    {
        const string Table = "InputLogs";

        await using var con = new SqliteConnection(conn);
        await con.OpenAsync(ct);

        // discover column names on Logs
        var cmd = con.CreateCommand();
        cmd.CommandText = $"PRAGMA table_info({Table});";
        var cols = new List<string>();
        await using (var rd = await cmd.ExecuteReaderAsync(ct))
            while (await rd.ReadAsync(ct)) cols.Add(rd.GetString(1));

        var names = new HashSet<string>(cols, StringComparer.OrdinalIgnoreCase);

        // REQUIRED in your new schema
        string[] required = { "TxHash", "LogIndex", "Address", "Topic0", "BlockNumber", "BlockTime" };
        var missing = required.Where(r => !names.Contains(r)).ToArray();
        if (missing.Length > 0)
            throw new InvalidOperationException($"Logs missing required column(s): {string.Join(",", missing)}");

        // OPTIONAL
        bool hasT1 = names.Contains("Topic1");
        bool hasT2 = names.Contains("Topic2");
        bool hasT3 = names.Contains("Topic3");
        bool hasData = names.Contains("Data");

        // final insert column order (only what's present)
        var ordered = new List<string> { "TxHash", "LogIndex", "Address", "Topic0", "BlockNumber", "BlockTime" };
        if (hasT1) ordered.Add("Topic1");
        if (hasT2) ordered.Add("Topic2");
        if (hasT3) ordered.Add("Topic3");
        if (hasData) ordered.Add("Data");

        // InsertMap only cares about whether Topics 1..3 exist; BlockTime handled directly
        var map = new InsertMap(hasTopic0: true, hasTopic1: hasT1, hasTopic2: hasT2, hasTopic3: hasT3, hasTimestamp: false);

        return new RawLogWriter(conn, Table, ordered.ToArray(), map, hasTopicsJson: false, hasData: hasData);
    }

    private sealed record Col(string Name, bool NotNull, string? DefaultValue);
    private static async Task<Col[]> GetColumnsAsync(SqliteConnection con, string table, CancellationToken ct)
    {
        var cmd = con.CreateCommand();
        cmd.CommandText = $"PRAGMA table_info({table});";
        var list = new List<Col>();
        await using var rd = await cmd.ExecuteReaderAsync(ct);
        while (await rd.ReadAsync(ct))
        {
            var name = rd.GetString(1);
            var notnull = rd.GetInt32(3) == 1;
            var dflt = rd.IsDBNull(4) ? null : rd.GetString(4);
            list.Add(new Col(name, notnull, dflt));
        }
        return list.ToArray();
    }

    internal readonly struct InsertMap(bool hasTopic0, bool hasTopic1, bool hasTopic2, bool hasTopic3, bool hasTimestamp)
    {
        public bool HasTopic0 { get; } = hasTopic0;
        public bool HasTopic1 { get; } = hasTopic1;
        public bool HasTopic2 { get; } = hasTopic2;
        public bool HasTopic3 { get; } = hasTopic3;
        public bool HasTimestamp { get; } = hasTimestamp;
    }
}