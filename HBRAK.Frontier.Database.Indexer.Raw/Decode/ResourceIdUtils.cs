using HBRAK.Frontier.Communication.Api.Data.Meta.AbisConfig;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace HBRAK.Frontier.Database.Indexer.Raw.Decode;


public sealed class ResourceIdInfo
{
    public string Hex { get; init; } = "0x";
    public string? Label { get; init; }  // e.g. "core:Ship" if known
    public ushort TypeId { get; init; }  // first 2 bytes (best-effort)
    public string NamespaceIdHex { get; init; } = "0x"; // middle 14 bytes
    public string NameIdHex { get; init; } = "0x";      // last 16 bytes
}

/// <summary>
/// Humanizes MUD ResourceIds using AbisConfigResponse (Contracts + Systems + Tables if present).
/// If no mapping is found, returns a structured fallback with type/ns/name hex.
/// </summary>
public static class ResourceIdUtils
{
    // Build mapping once at startup and reuse
    public static Dictionary<string, string> BuildLabelMap(AbisConfigResponse cfg)
    {
        var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        // Try common carriers that might expose ResourceId / TableId
        foreach (var (carrierName, coll) in EnumerateNamedCollections(cfg, "Contracts", "Systems", "Tables"))
        {
            foreach (var item in coll)
            {
                if (item is null) continue;

                // HEX id property candidates
                var idHex = FirstNonEmptyString(item, "ResourceId", "TableId", "Id");
                if (string.IsNullOrWhiteSpace(idHex) || idHex!.Length < 4) continue;

                // label property candidates (namespace + name if available)
                var ns = FirstNonEmptyString(item, "Namespace", "Ns", "Module", "Package");
                var nam = FirstNonEmptyString(item, "Name", "TableName", "SystemName");
                var label = !string.IsNullOrWhiteSpace(ns) && !string.IsNullOrWhiteSpace(nam)
                    ? $"{ns}:{nam}"
                    : nam ?? ns ?? carrierName;

                dict[Normalize0x(idHex)] = label!;
            }
        }

        return dict;
    }

    public static ResourceIdInfo Describe(string resourceIdHex, IReadOnlyDictionary<string, string>? labelMap = null)
    {
        var hex = Normalize0x(resourceIdHex);
        var bytes = HexToBytes(hex);

        ushort typeId = 0;
        if (bytes.Length >= 2)
            typeId = (ushort)((bytes[0] << 8) | bytes[1]);

        // common packing: [0..1]=type (2B), [2..15]=ns (14B), [16..31]=name (16B)
        ReadOnlySpan<byte> nsSpan = bytes.Length >= 16 ? bytes.AsSpan(2, Math.Min(14, bytes.Length - 2)) : ReadOnlySpan<byte>.Empty;
        ReadOnlySpan<byte> nameSpan = bytes.Length >= 32 ? bytes.AsSpan(16, Math.Min(16, bytes.Length - 16)) : ReadOnlySpan<byte>.Empty;

        string nsHex = To0x(nsSpan);
        string nameHex = To0x(nameSpan);

        // try label map first
        string? label = null;
        if (labelMap != null && labelMap.TryGetValue(hex, out var found))
            label = found;

        // ascii fallback if no label
        if (label is null)
        {
            static string TryAscii(ReadOnlySpan<byte> s)
            {
                if (s.IsEmpty) return "";
                var trimmed = s.ToArray().Where(b => b != 0).ToArray();
                if (trimmed.Length == 0) return "";
                if (trimmed.All(b => b >= 32 && b <= 126))
                    return Encoding.ASCII.GetString(trimmed);
                return "";
            }
            var nsAscii = TryAscii(nsSpan);
            var nameAscii = TryAscii(nameSpan);
            if (nsAscii.Length > 0 && nameAscii.Length > 0)
                label = $"{nsAscii}:{nameAscii}";
            else if (nameAscii.Length > 0)
                label = nameAscii;
        }

        return new ResourceIdInfo
        {
            Hex = hex,
            Label = label,
            TypeId = typeId,
            NamespaceIdHex = nsHex,
            NameIdHex = nameHex
        };
    }


    public static string Humanize(string resourceIdHex, IReadOnlyDictionary<string, string>? labelMap = null)
    {
        var info = Describe(resourceIdHex, labelMap);
        if (!string.IsNullOrWhiteSpace(info.Label))
            return info.Label!;

        return $"type=0x{info.TypeId:X4} ns={info.NamespaceIdHex} name={info.NameIdHex}";
    }

    // ---------- helpers ----------

    private static IEnumerable<(string name, IEnumerable collection)> EnumerateNamedCollections(object root, params string[] propertyNames)
    {
        foreach (var n in propertyNames)
        {
            var p = root.GetType().GetProperty(n, BindingFlags.Public | BindingFlags.Instance);
            if (p?.GetValue(root) is IEnumerable e)
                yield return (n, e);
        }
    }

    private static string? FirstNonEmptyString(object obj, params string[] names)
    {
        foreach (var n in names)
        {
            var p = obj.GetType().GetProperty(n, BindingFlags.Public | BindingFlags.Instance);
            if (p == null) continue;
            var v = p.GetValue(obj);
            if (v is string s && !string.IsNullOrWhiteSpace(s))
                return s;
        }
        return null;
    }

    private static string Normalize0x(string s)
    {
        s = s.Trim();
        if (!s.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            s = "0x" + s;
        return s.ToLowerInvariant();
    }

    private static byte[] HexToBytes(string hex)
    {
        if (hex.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            hex = hex[2..];
        int len = hex.Length / 2;
        var data = new byte[len];
        for (int i = 0; i < len; i++)
            data[i] = byte.Parse(hex.AsSpan(2 * i, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
        return data;
    }

    private static string To0x(ReadOnlySpan<byte> bytes)
    {
        char[] c = new char[bytes.Length * 2 + 2];
        c[0] = '0'; c[1] = 'x';
        int j = 2;
        for (int i = 0; i < bytes.Length; i++)
        {
            var b = bytes[i];
            c[j++] = GetHexNibble(b >> 4);
            c[j++] = GetHexNibble(b & 0xF);
        }
        return new string(c).ToLowerInvariant();
    }

    private static char GetHexNibble(int val) => (char)(val < 10 ? '0' + val : 'a' + (val - 10));
}

