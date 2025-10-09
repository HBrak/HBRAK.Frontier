using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HBRAK.Frontier.Database.Indexer.Raw.Objects;

public sealed class RawCursor
{
    public int Id { get; set; } = 1; // always 1
    public long LastProcessedBlock { get; set; } // watermark
    public long? LastFinalizedBlock { get; set; } // optional
    public long ChainId { get; set; }
}
