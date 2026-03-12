using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HBRAK.Frontier.Database.Indexer.Raw;

public interface IRawIndexer
{
    Task<bool> RunOnceAsync(CancellationToken ct);
}
