using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HBRAK.Frontier.Database.Indexer.Raw.Objects;

public abstract class LogRowBase
{
    public byte[] TxHash { get; set; } = Array.Empty<byte>();
    public int LogIndex { get; set; }

    public byte[] Address { get; set; } = Array.Empty<byte>();

    public byte[] Topic0 { get; set; } = Array.Empty<byte>();
    public byte[]? Topic1 { get; set; }
    public byte[]? Topic2 { get; set; }
    public byte[]? Topic3 { get; set; }

    public byte[]? Data { get; set; }

    public long BlockNumber { get; set; }
    public long BlockTime { get; set; } // unix seconds
}

[Table("InputLogs")]
public sealed class InputLogRow : LogRowBase { }

[Table("UnableToParseLogs")]
public sealed class UnableToParseLogRow : LogRowBase { }
