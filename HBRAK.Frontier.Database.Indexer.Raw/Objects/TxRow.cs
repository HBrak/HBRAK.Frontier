using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HBRAK.Frontier.Database.Indexer.Raw.Objects;

public sealed class TxRow
{
    public string Hash { get; set; } = ""; // PK
    public long BlockNumber { get; set; }
    public int IndexInBlock { get; set; }
    public string From { get; set; } = "";
    public string? To { get; set; }
    public string InputHex { get; set; } = "";
}