using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static HBRAK.Frontier.Database.Indexer.Raw.Decode.MudStuff;

namespace HBRAK.Frontier.Database.Indexer.Raw.Decode;

internal class MudRecordDecoder
{
    /// <summary>
    /// Decode a full record (SetRecord) using schema. Returns human-friendly key/value pairs.
    /// For Splice* you typically need to first apply patches to reconstruct the bytes; this quick helper
    /// will decode whatever static/dynamic data is present on the event.
    /// </summary>
    public static (Dictionary<string, string> Key, Dictionary<string, string> Fields) DecodeSetRecord(
        MudStoreEvent ev, MudTableSchema schema)
    {
        var key = DecodeKeys(ev.KeyTupleHex, schema.Key);
        var fields = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        // Static: contiguous bytes; each field occupies its fixed size (packed in order, left-padded to 32 on-chain)
        var staticBytes = HexToBytes(ev.StaticDataHex);
        int off = 0;
        foreach (var f in schema.StaticFields)
        {
            var width = Math.Min(f.FixedSizeBytes <= 0 ? 32 : f.FixedSizeBytes, Math.Max(0, staticBytes.Length - off));
            var slice = width == 0 ? Array.Empty<byte>() : staticBytes.AsSpan(off, width).ToArray();
            fields[f.Name] = HumanValue(f.Type, slice);
            off += width;
        }

        // Dynamic: we rely on encodedLengths to split the dynamicData across the dynamic fields in order
        var dynData = HexToBytes(ev.DynamicDataHex);
        var dynLens = DecodePackedLengths(ev.EncodedLengthsHex, schema.DynamicFields.Count);
        int dynOff = 0;
        for (int i = 0; i < schema.DynamicFields.Count; i++)
        {
            var f = schema.DynamicFields[i];
            var len = (i < dynLens.Length) ? dynLens[i] : 0;
            len = Math.Min(len, Math.Max(0, dynData.Length - dynOff));
            var slice = len == 0 ? Array.Empty<byte>() : dynData.AsSpan(dynOff, len).ToArray();
            fields[f.Name] = HumanValue(f.Type, slice);
            dynOff += len;
        }

        return (key, fields);
    }

    /// <summary>Decode keys (each key is a bytes32 in the tuple) into human-friendly values using expected types.</summary>
    public static Dictionary<string, string> DecodeKeys(string[] keyTupleHex, List<MudField> keySchema)
    {
        var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        for (int i = 0; i < Math.Min(keyTupleHex.Length, keySchema.Count); i++)
        {
            var raw = HexToBytes(keyTupleHex[i]);
            var f = keySchema[i];

            // keys are bytes32 on-chain; cast into expected type
            // address: last 20 bytes; uint/int: interpret big-endian; string: ascii if printable
            string val;
            if (f.Type.Equals("address", StringComparison.OrdinalIgnoreCase))
            {
                var addr = new byte[20];
                Buffer.BlockCopy(raw, 12, addr, 0, 20);
                val = ToChecksum("0x" + BytesToHex(addr));
            }
            else if (f.Type.StartsWith("uint", StringComparison.OrdinalIgnoreCase))
            {
                val = ToUInt256(raw).ToString();
            }
            else if (f.Type.StartsWith("int", StringComparison.OrdinalIgnoreCase))
            {
                val = ToInt256(raw).ToString();
            }
            else
            {
                // try ASCII
                var ascii = Encoding.ASCII.GetString(raw.Where(x => x != 0).ToArray());
                if (ascii.Length > 0 && ascii.All(ch => ch >= 32 && ch <= 126))
                    val = ascii;
                else
                    val = $"bytes32({raw.Length}B)";
            }

            dict[f.Name.Length == 0 ? $"key{i}" : f.Name] = val;
        }

        return dict;
    }

    // ---------- helpers ----------

    public static string HumanValue(string type, byte[] bytes)
    {
        if (type.Equals("string", StringComparison.OrdinalIgnoreCase))
            return AsciiOrBytes(bytes);
        if (type.Equals("bytes", StringComparison.OrdinalIgnoreCase))
            return $"bytes({bytes.Length})";

        if (type.Equals("address", StringComparison.OrdinalIgnoreCase))
        {
            if (bytes.Length < 20) return $"addr?(len={bytes.Length})";
            var addr = new byte[20]; Buffer.BlockCopy(bytes, Math.Max(0, bytes.Length - 20), addr, 0, 20);
            return ToChecksum("0x" + BytesToHex(addr));
        }

        if (type.StartsWith("uint", StringComparison.OrdinalIgnoreCase))
            return ToUintDecimal(bytes);

        if (type.StartsWith("int", StringComparison.OrdinalIgnoreCase))
            return ToIntDecimal(bytes);

        if (type.StartsWith("bytes", StringComparison.OrdinalIgnoreCase))
        {
            // bytesN
            return $"bytes{bytes.Length}";
        }

        // default: try ascii then bytes
        return AsciiOrBytes(bytes);
    }

    // PackedCounter heuristic: up to 6 counters of 5 bytes each, concatenated from left to right.
    private static int[] DecodePackedLengths(string? hex32, int count)
    {
        if (string.IsNullOrWhiteSpace(hex32) || count <= 0) return Array.Empty<int>();
        var b = HexToBytes(hex32);
        if (b.Length != 32) return Array.Empty<int>();

        var sizes = new List<int>(count);
        int off = 0;
        for (int i = 0; i < count && off + 5 <= 32; i++, off += 5)
        {
            // take 5 bytes big-endian
            ulong val = 0;
            for (int k = 0; k < 5; k++) val = (val << 8) | b[off + k];
            sizes.Add((int)Math.Min(int.MaxValue, (long)val));
        }
        return sizes.ToArray();
    }

    private static string AsciiOrBytes(byte[] bytes)
    {
        if (bytes.Length == 0) return "''";
        int print = bytes.Count(ch => ch >= 32 && ch <= 126);
        if (print >= Math.Min(16, bytes.Length))
        {
            var s = new string(bytes.Select(ch => (ch >= 32 && ch <= 126) ? (char)ch : '·').ToArray());
            if (s.Length > 64) s = s[..64] + "…";
            return $"\"{s}\"";
        }
        return $"bytes({bytes.Length})";
    }

    private static System.Numerics.BigInteger ToUInt256(byte[] be32)
        => new System.Numerics.BigInteger(be32.Reverse().Concat(new byte[] { 0 }).ToArray()); // unsigned

    private static System.Numerics.BigInteger ToInt256(byte[] be32)
        => new System.Numerics.BigInteger(be32.Reverse().ToArray()); // two's complement

    private static string ToUintDecimal(byte[] bigEndian)
        => new System.Numerics.BigInteger(bigEndian.Reverse().Concat(new byte[] { 0 }).ToArray()).ToString();

    private static string ToIntDecimal(byte[] bigEndian)
        => new System.Numerics.BigInteger(bigEndian.Reverse().ToArray()).ToString();

    private static byte[] HexToBytes(string? hex)
    {
        if (string.IsNullOrWhiteSpace(hex)) return Array.Empty<byte>();
        if (hex.StartsWith("0x", StringComparison.OrdinalIgnoreCase)) hex = hex[2..];
        int len = hex.Length / 2;
        var data = new byte[len];
        for (int i = 0; i < len; i++) data[i] = Convert.ToByte(hex.Substring(i * 2, 2), 16);
        return data;
    }

    private static string BytesToHex(byte[] bytes)
    {
        char[] c = new char[bytes.Length * 2];
        for (int i = 0; i < bytes.Length; i++)
        {
            var b = bytes[i];
            c[i * 2] = GetHexNibble(b >> 4);
            c[i * 2 + 1] = GetHexNibble(b & 0xF);
        }
        return new string(c).ToLowerInvariant();
    }

    private static char GetHexNibble(int v) => (char)(v < 10 ? '0' + v : 'a' + (v - 10));

    private static string ToChecksum(string lowerHex)
    {
        // lightweight: return lower for now; if you already reference Nethereum.Util, swap with AddressUtil for checksum.
        return lowerHex;
    }
}

