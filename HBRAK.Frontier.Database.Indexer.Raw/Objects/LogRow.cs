using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HBRAK.Frontier.Database.Indexer.Raw.Objects;

public sealed class LogRow
{
    public string TxHash { get; set; } = ""; // PK part
    public int LogIndex { get; set; } // PK part
    public string Address { get; set; } = "";
    public string Topic0 { get; set; } = "";
    public string[] Topics { get; set; } = Array.Empty<string>();
    public byte[] Data { get; set; } = Array.Empty<byte>();
    public long BlockNumber { get; set; }
    public DateTimeOffset BlockTime { get; set; }
}
