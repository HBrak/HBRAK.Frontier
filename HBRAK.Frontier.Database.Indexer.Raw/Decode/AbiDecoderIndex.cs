using HBRAK.Frontier.Communication.Api.Data.Meta.AbisConfig;
using HBRAK.Frontier.Database.Indexer.Raw.Objects;
using Nethereum.Util;
using System.Globalization;
using System.Numerics;
using System.Text;

namespace HBRAK.Frontier.Database.Indexer.Raw.Decode;

internal class AbiDecoderIndex
{
    // addressHex ("0x..") -> topic0Hex ("0x..") -> compiled event
    private readonly Dictionary<string, Dictionary<string, AbiEvent>> _index =
        new(StringComparer.OrdinalIgnoreCase);

    public static AbiDecoderIndex BuildFrom(AbisConfigResponse abiscfg)
    {
        var ix = new AbiDecoderIndex();

        // TODO: adjust if your names differ:
        // assume cfg.Contracts is IEnumerable<ContractItem>
        // ContractItem has string DeployedTo, IEnumerable<AbiItem> Abi
        foreach (var c in abiscfg.Cfg) // <-- adjust
        {
            if (string.IsNullOrWhiteSpace(c.DeployedTo)) continue;
            var addrHex = NormalizeAddr(c.DeployedTo);

            foreach (var entry in c.Abi) // <-- adjust
            {
                if (!string.Equals(entry.Type, "event", StringComparison.OrdinalIgnoreCase))
                    continue;

                var ev = new AbiEvent
                {
                    Name = entry.Name,
                    Anonymous = entry.Anonymous ?? false,
                    Inputs = entry.Inputs.Select(i => new AbiInput
                    {
                        Name = i.Name,
                        Type = CanonicalType(i.Type),
                        Indexed = i.Indexed ?? false
                    }).ToList()
                };

                // compute topic0 = keccak256("Name(type1,type2,...)")
                // anonymous events have no topic0; skip those for topic-index
                if (!ev.Anonymous)
                {
                    var sig = BuildEventSignature(ev.Name, ev.Inputs);
                    var t0 = "0x" + ToHex(Sha3Keccack.Current.CalculateHash(Encoding.ASCII.GetBytes(sig)));
                    ix._index.TryAdd(addrHex, new(StringComparer.OrdinalIgnoreCase));
                    ix._index[addrHex][t0] = ev;
                }
            }
        }

        return ix;
    }

    /// Decode ONE log row. Returns null if no ABI match.
    public DecodedLog? TryDecode(LogRowBase raw)
    {
        var addrHex = "0x" + ToHex(raw.Address);
        if (!_index.TryGetValue(addrHex, out var topicMap)) return null;

        var topic0Hex = raw.Topic0 is { Length: > 0 } ? "0x" + ToHex(raw.Topic0) : null;
        if (topic0Hex == null) return null; // anonymous events not indexed here

        if (!topicMap.TryGetValue(topic0Hex, out var ev)) return null;

        // Split inputs
        var indexed = ev.Inputs.Where(i => i.Indexed).ToList();
        var nonIdx = ev.Inputs.Where(i => !i.Indexed).ToList();

        // Collect topic words in order for indexed inputs (topic1..topic3)
        var topicWords = new List<byte[]>();
        if (raw.Topic1 is not null) topicWords.Add(raw.Topic1);
        if (raw.Topic2 is not null) topicWords.Add(raw.Topic2);
        if (raw.Topic3 is not null) topicWords.Add(raw.Topic3);
        int tcur = 0;

        var args = new Dictionary<string, object?>(ev.Inputs.Count, StringComparer.OrdinalIgnoreCase);

        // decode indexed
        foreach (var inp in ev.Inputs)
        {
            if (!inp.Indexed) continue;
            if (tcur >= topicWords.Count) { args[NameOrPos(inp)] = null; continue; }
            var word = topicWords[tcur++];

            if (IsDynamicType(inp.Type))
            {
                // only hash is available for indexed dynamic
                args[NameOrPos(inp)] = new Dictionary<string, object?>
                {
                    ["__indexed_hash__"] = "0x" + ToHex(word)
                };
            }
            else
            {
                args[NameOrPos(inp)] = DecodeStaticWord(inp.Type, word);
            }
        }

        // decode non-indexed from data
        var data = raw.Data ?? Array.Empty<byte>();
        if (nonIdx.Count > 0 && data.Length > 0)
        {
            var decoded = DecodeNonIndexed(nonIdx, data);
            foreach (var kv in decoded) args[kv.Key] = kv.Value;
        }

        return new DecodedLog
        {
            EventName = ev.Name,
            BlockNumber = raw.BlockNumber,
            BlockTime = raw.BlockTime,
            TxHashHex = "0x" + ToHex(raw.TxHash),
            LogIndex = raw.LogIndex,
            Args = args
        };
    }

    // ---------- helpers ----------

    private static string NormalizeAddr(string s)
        => s.StartsWith("0x", StringComparison.OrdinalIgnoreCase) ? s.ToLowerInvariant() : "0x" + s.ToLowerInvariant();

    private static string BuildEventSignature(string name, List<AbiInput> inputs)
        => $"{name}({string.Join(",", inputs.Select(i => i.Type))})";

    private static string CanonicalType(string t)
    {
        t = t.Trim();
        if (t.Equals("uint", StringComparison.OrdinalIgnoreCase)) return "uint256";
        if (t.Equals("int", StringComparison.OrdinalIgnoreCase)) return "int256";
        return t;
    }

    private static string NameOrPos(AbiInput inp, int i = -1)
        => string.IsNullOrWhiteSpace(inp.Name) ? (i >= 0 ? $"arg{i}" : "arg") : inp.Name;

    private static bool IsDynamicType(string type)
    {
        type = type.Trim();
        if (type.Equals("string", StringComparison.OrdinalIgnoreCase)) return true;
        if (type.Equals("bytes", StringComparison.OrdinalIgnoreCase)) return true;
        if (type.EndsWith("[]", StringComparison.Ordinal)) return true;
        return false;
    }

    private static object? DecodeStaticWord(string type, byte[] word)
    {
        type = type.Trim();

        if (type.Equals("address", StringComparison.OrdinalIgnoreCase))
        {
            var addr = new byte[20];
            Buffer.BlockCopy(word, 12, addr, 0, 20);
            return "0x" + ToHex(addr);
        }

        if (type.Equals("bool", StringComparison.OrdinalIgnoreCase))
            return word[^1] != 0;

        if (type.StartsWith("uint", StringComparison.OrdinalIgnoreCase))
            return ToUInt256(word).ToString();

        if (type.StartsWith("int", StringComparison.OrdinalIgnoreCase))
            return ToInt256(word).ToString();

        // Handle fixed-size bytesN (1..32), including bytes32
        if (type.StartsWith("bytes", StringComparison.OrdinalIgnoreCase))
        {
            // dynamic "bytes" (no N) is NOT static; it's decoded from DATA tail elsewhere
            if (type.Equals("bytes", StringComparison.OrdinalIgnoreCase))
                return "/* dynamic 'bytes' is not a static word */";

            int len;
            if (type.Equals("bytes32", StringComparison.OrdinalIgnoreCase))
            {
                len = 32;
            }
            else if (!TryParseFixedBytes(type, out len))
            {
                return "/* unsupported static type: " + type + " */";
            }

            var slice = new byte[len];
            Buffer.BlockCopy(word, 0, slice, 0, len);
            return "0x" + ToHex(slice);
        }

        return "/* unsupported static type: " + type + " */";
    }


    private static Dictionary<string, object?> DecodeNonIndexed(List<AbiInput> inputs, byte[] data)
    {
        var result = new Dictionary<string, object?>(inputs.Count, StringComparer.OrdinalIgnoreCase);

        // heads (32B words)
        var heads = new List<(AbiInput inp, byte[] word)>(inputs.Count);
        for (int i = 0; i < inputs.Count; i++)
            heads.Add((inputs[i], ReadWord(data, i)));

        var tails = new List<(AbiInput inp, int off)>();

        // first pass
        for (int i = 0; i < heads.Count; i++)
        {
            var (inp, word) = heads[i];
            if (!IsDynamicType(inp.Type))
                result[NameOrPos(inp, i)] = DecodeStaticWord(inp.Type, word);
            else
                tails.Add((inp, (int)ToUInt256(word)));
        }

        // second pass for dynamic
        foreach (var (inp, off) in tails)
        {
            if (off < 0 || off + 32 > data.Length) { result[NameOrPos(inp)] = null; continue; }
            var len = (int)ToUInt256(ReadWordAt(data, off));
            var start = off + 32;
            if (start + len > data.Length) { result[NameOrPos(inp)] = null; continue; }

            var slice = new byte[len];
            Buffer.BlockCopy(data, start, slice, 0, len);

            if (inp.Type.Equals("string", StringComparison.OrdinalIgnoreCase))
                result[NameOrPos(inp)] = Encoding.UTF8.GetString(slice);
            else if (inp.Type.Equals("bytes", StringComparison.OrdinalIgnoreCase))
                result[NameOrPos(inp)] = "0x" + ToHex(slice);
            else
                result[NameOrPos(inp)] = "/* unsupported dynamic type: " + inp.Type + " */";
        }

        return result;
    }

    private static byte[] ReadWord(byte[] data, int wordIndex)
    {
        var word = new byte[32];
        int off = wordIndex * 32;
        if (off + 32 <= data.Length) Buffer.BlockCopy(data, off, word, 0, 32);
        return word;
    }

    private static byte[] ReadWordAt(byte[] data, int offset)
    {
        var word = new byte[32];
        if (offset + 32 <= data.Length) Buffer.BlockCopy(data, offset, word, 0, 32);
        return word;
    }

    private static BigInteger ToUInt256(byte[] word)
    {
        var copy = (byte[])word.Clone();
        Array.Reverse(copy);
        return new BigInteger(copy, isUnsigned: true, isBigEndian: false);
    }

    private static BigInteger ToInt256(byte[] word)
    {
        var copy = (byte[])word.Clone();
        Array.Reverse(copy);
        return new BigInteger(copy, isUnsigned: false, isBigEndian: false);
    }

    private static string ToHex(byte[] bytes)
    {
        char[] c = new char[bytes.Length * 2];
        int b;
        for (int i = 0; i < bytes.Length; i++)
        {
            b = bytes[i] >> 4;
            c[i * 2] = (char)(55 + b + (((b - 10) >> 31) & -7));
            b = bytes[i] & 0xF;
            c[i * 2 + 1] = (char)(55 + b + (((b - 10) >> 31) & -7));
        }
        return new string(c);
    }

    private static bool TryParseFixedBytes(string type, out int n)
    {
        n = 0;

        // Expect forms like "bytes1" .. "bytes32"
        if (!type.StartsWith("bytes", StringComparison.OrdinalIgnoreCase))
            return false;

        var span = type.AsSpan(5); // part after "bytes"
        if (span.Length == 0)
            return false;

        if (!int.TryParse(span, NumberStyles.None, CultureInfo.InvariantCulture, out var val))
            return false;

        if (val < 1 || val > 32)
            return false;

        n = val;
        return true;
    }
}