using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HBRAK.Frontier.Database.Indexer.Raw.Decode;

public sealed class AbiEvent
{
    public string Name { get; init; } = "";
    public bool Anonymous { get; init; }
    public List<AbiInput> Inputs { get; init; } = new();
    public byte[] Topic0 { get; init; } = Array.Empty<byte>();
}
