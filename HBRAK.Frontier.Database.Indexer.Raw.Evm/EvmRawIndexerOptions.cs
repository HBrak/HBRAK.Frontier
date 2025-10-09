using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HBRAK.Frontier.Database.Indexer.Raw.Evm;

public sealed class EvmRawIndexerOptions
{
    public string RpcUrl { get; set; } = "https://pyrope-external-sync-node-rpc.live.tech.evefrontier.com";
    public long ChainId { get; set; } = 695569;
    public long StartBlock { get; set; } = 0;
    public int BatchSize { get; set; } = 2_000;
    public int Confirmations { get; set; } = 12;
    public string[] Addresses { get; set; } = [];
}
