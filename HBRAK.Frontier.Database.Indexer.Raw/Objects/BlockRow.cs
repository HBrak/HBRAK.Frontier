using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HBRAK.Frontier.Database.Indexer.Raw.Objects;

public sealed class BlockRow
{
    public long Number { get; set; } //PK
    public string Hash { get; set; } = "";
    public string ParentHash { get; set; } = "";
    public DateTimeOffset Timestamp { get; set; }
}
