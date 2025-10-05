using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HBRAK.Frontier.Chain.Service;

public class ChainServiceOptions
{
    public string RpcUrl { get; set; } = "https://pyrope-external-sync-node-rpc.live.tech.evefrontier.com";
    public long? ChainId { get; set; } = 695569;
    public int TimeoutSeconds { get; set; } = 20;
}
