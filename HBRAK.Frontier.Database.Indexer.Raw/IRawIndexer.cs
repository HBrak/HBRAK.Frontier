using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HBRAK.Frontier.Database.Indexer.Raw;

public interface IRawIndexer
{
    Task<long> GetLastProcessedAsync(CancellationToken ct = default);
    Task<bool> RunOnceAsync(CancellationToken ct = default);
}
