using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HBRAK.Frontier.Database.Indexer.Raw.Decode;

public sealed class AbiInput
{
    public string Name { get; init; } = "";
    public string Type { get; init; } = "";
    public bool Indexed { get; init; }
}