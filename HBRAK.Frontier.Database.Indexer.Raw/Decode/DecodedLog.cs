using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HBRAK.Frontier.Database.Indexer.Raw.Decode;

public sealed class DecodedLog
{
    public string EventName { get; init; } = "";
    public long BlockNumber { get; init; }
    public long BlockTime { get; init; }
    public string TxHashHex { get; init; } = "0x";
    public int LogIndex { get; init; }
    public Dictionary<string, object?> Args { get; init; } = new();
}
