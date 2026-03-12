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

// Basic field model
public sealed class MudField
{
    public string Name { get; init; } = "";
    public string Type { get; init; } = "";   // e.g., "address", "uint32", "bytes32", "string", "bytes"
    public bool IsDynamic { get; init; }    // true for string/bytes (this minimal version targets these)
    public int FixedSizeBytes { get; init; } // for static types
}

// Table schema: list of key fields, static fields, dynamic fields
public sealed class MudTableSchema
{
    public string TableIdHex { get; init; } = "0x";
    public string Label { get; init; } = ""; // e.g. "namespace:name" if known
    public List<MudField> Key { get; init; } = new();
    public List<MudField> StaticFields { get; init; } = new();
    public List<MudField> DynamicFields { get; init; } = new();
}
public static class MudSchemaRegistry
{
    public static Dictionary<string, MudTableSchema> Build(AbisConfigResponse cfg)
    {
        var result = new Dictionary<string, MudTableSchema>(StringComparer.OrdinalIgnoreCase);

        foreach (var item in EnumeratePotentialTables(cfg))
        {
            var tableId = GetString(item, "ResourceId") ?? GetString(item, "TableId") ?? "";
            if (string.IsNullOrWhiteSpace(tableId)) continue;

            var ns = GetString(item, "Namespace") ?? GetString(item, "Ns") ?? "";
            var name = GetString(item, "Name") ?? GetString(item, "TableName") ?? "";

            var label = (!string.IsNullOrWhiteSpace(ns) && !string.IsNullOrWhiteSpace(name))
                            ? $"{ns}:{name}"
                            : name;

            // Key schema
            var key = new List<MudField>();
            var keySchema = GetValue(item, "KeySchema") ?? GetValue(item, "Key");
            if (keySchema is not null)
                key.AddRange(ReadFields(keySchema));

            // Value schema (static + dynamic lists)
            var staticFields = new List<MudField>();
            var dynamicFields = new List<MudField>();
            var valueSchema = GetValue(item, "ValueSchema") ?? GetValue(item, "Value");
            if (valueSchema is not null)
            {
                var staticsObj = GetValue(valueSchema, "Static") ?? GetValue(valueSchema, "Statics");
                var dynamicsObj = GetValue(valueSchema, "Dynamic") ?? GetValue(valueSchema, "Dynamics");
                staticFields.AddRange(ReadFields(staticsObj));
                dynamicFields.AddRange(ReadFields(dynamicsObj, isDynamic: true));
            }

            var schema = new MudTableSchema
            {
                TableIdHex = Normalize0x(tableId),
                Label = label ?? "",
                Key = key,
                StaticFields = staticFields,
                DynamicFields = dynamicFields,
            };

            // don't add empty schemas (no fields at all)
            if (schema.Key.Count + schema.StaticFields.Count + schema.DynamicFields.Count == 0)
                continue;

            result[schema.TableIdHex] = schema;
        }

        return result;
    }

    private static IEnumerable<object> EnumeratePotentialTables(object root)
    {
        foreach (var propName in new[] { "Tables", "Contracts", "Systems" })
        {
            var p = root.GetType().GetProperty(propName, BindingFlags.Public | BindingFlags.Instance);
            if (p?.GetValue(root) is IEnumerable seq)
                foreach (var item in seq)
                    if (item is not null)
                        yield return item;
        }
    }

    private static IEnumerable<MudField> ReadFields(object? schemaObj, bool isDynamic = false)
    {
        if (schemaObj is null) yield break;

        if (schemaObj is IEnumerable list)
        {
            foreach (var f in list)
            {
                var name = GetString(f!, "Name") ?? "";
                var type = Canonical(GetString(f!, "Type") ?? "");
                if (string.IsNullOrWhiteSpace(type)) continue;

                var size = isDynamic ? 0 : FixedSizeOf(type);
                yield return new MudField { Name = name, Type = type, IsDynamic = isDynamic || IsDynamicType(type), FixedSizeBytes = size };
            }
        }
        else
        {
            // Sometimes schema may be an object with { Names:[...], Types:[...] }
            var names = GetValue(schemaObj, "Names") as IEnumerable;
            var types = GetValue(schemaObj, "Types") as IEnumerable;
            if (names is null || types is null) yield break;

            var nList = names.Cast<object>().Select(x => x?.ToString() ?? "").ToArray();
            var tList = types.Cast<object>().Select(x => Canonical(x?.ToString() ?? "")).ToArray();
            for (int i = 0; i < Math.Min(nList.Length, tList.Length); i++)
            {
                var t = tList[i];
                var size = isDynamic ? 0 : FixedSizeOf(t);
                yield return new MudField { Name = nList[i], Type = t, IsDynamic = isDynamic || IsDynamicType(t), FixedSizeBytes = size };
            }
        }
    }

    private static string Canonical(string t)
    {
        t = t.Trim();
        if (t.Equals("uint", StringComparison.OrdinalIgnoreCase)) return "uint256";
        if (t.Equals("int", StringComparison.OrdinalIgnoreCase)) return "int256";
        return t;
    }

    private static bool IsDynamicType(string t)
        => t.Equals("string", StringComparison.OrdinalIgnoreCase) || t.Equals("bytes", StringComparison.OrdinalIgnoreCase) || t.EndsWith("[]", StringComparison.Ordinal);

    private static int FixedSizeOf(string t)
    {
        if (t.Equals("bool", StringComparison.OrdinalIgnoreCase)) return 1;
        if (t.Equals("address", StringComparison.OrdinalIgnoreCase)) return 20;
        if (t.StartsWith("uint", StringComparison.OrdinalIgnoreCase) || t.StartsWith("int", StringComparison.OrdinalIgnoreCase))
        {
            var n = 256;
            var s = t.Substring(t[0] == 'u' || t[0] == 'U' ? 4 : 3);
            if (int.TryParse(s, NumberStyles.None, CultureInfo.InvariantCulture, out var bits) && bits > 0) n = bits;
            return n / 8;
        }
        if (t.StartsWith("bytes", StringComparison.OrdinalIgnoreCase) && t.Length > 5)
        {
            if (int.TryParse(t.Substring(5), NumberStyles.None, CultureInfo.InvariantCulture, out var n) && n >= 1 && n <= 32)
                return n;
        }
        if (t.Equals("bytes32", StringComparison.OrdinalIgnoreCase)) return 32;
        return 32; // default to 32 for any fixed ABI slot (safe upper bound)
    }

    private static object? GetValue(object obj, string name)
        => obj.GetType().GetProperty(name, BindingFlags.Public | BindingFlags.Instance)?.GetValue(obj);

    private static string? GetString(object obj, string name)
        => GetValue(obj, name) as string;

    private static string Normalize0x(string s)
        => s.StartsWith("0x", StringComparison.OrdinalIgnoreCase) ? s.ToLowerInvariant() : "0x" + s.ToLowerInvariant();
}

