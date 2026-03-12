using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HBRAK.Frontier.Database.Indexer.Raw.Evm.Indexer;

public sealed class EvmRawIndexerOptions
{
    public string RpcUrl { get; set; } = "https://pyrope-external-sync-node-rpc.live.tech.evefrontier.com";
    public string? FilterAddress { get; set; } = null;
    public string? FilterTopic0 { get; set; } = null;
    public int DegreeOfParallelism { get; set; } = 8;
    public int BatchInsertSize { get; set; } = 10_000;
    public int Confirmations { get; set; } = 24;
    public long InitialRangeSpan { get; set; } = 2_000;
    public long MinRangeSpan { get; set; } = 100;
    public long MaxRangeSpan { get; set; } = 20_000;
    public bool EnrichWithBlockTimestamps { get; set; } = false;
    public SqliteTuning Sqlite { get; set; } = new();
    public string SqlitePath { get; set; } = "%LocalAppData%\\HBRAK.Frontier\\Db\\frontier_raw.db";
}
