using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HBRAK.Frontier.Database.Indexer.Raw.Evm;

public class SqliteTuning
{
    public bool Wal { get; set; } = true;               // PRAGMA journal_mode=WAL
    public string Synchronous { get; set; } = "OFF";    // OFF | NORMAL | FULL
    public int CacheSizePages { get; set; } = -200000;  // negative => KB
    public long MMapSizeBytes { get; set; } = 1L << 30; // 1 GiB
}
