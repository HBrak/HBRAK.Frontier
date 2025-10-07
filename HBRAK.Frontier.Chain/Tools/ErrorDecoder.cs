using Nethereum.ABI.FunctionEncoding;
using Nethereum.ABI.Model;
using Nethereum.Hex.HexConvertors.Extensions;
using Nethereum.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace HBRAK.Frontier.Chain.Tools;

public static class ErrorDecoder
{
    /// <summary>
    /// Tries to map encoded custom error data to an error name by scanning the ABI JSON
    /// for entries with "type":"error" and matching selectors (first 4 bytes of keccak(signature)).
    /// Returns (name, selector (8 hex chars), signature) or (null, selector, null) if not found.
    /// </summary>
    public static (string? name, string selector, string? signature)
        TryDecodeName(string abiJson, string? exceptionEncodedData)
    {
        // Normalize and extract selector from revert data
        if (string.IsNullOrWhiteSpace(exceptionEncodedData))
            return (null, "", null);

        var hex = exceptionEncodedData.StartsWith("0x", StringComparison.OrdinalIgnoreCase)
            ? exceptionEncodedData
            : "0x" + exceptionEncodedData;

        var bytes = hex.HexToByteArray();
        if (bytes.Length < 4) return (null, "", null);

        var selector = BitConverter.ToString(bytes, 0, 4).Replace("-", "").ToLowerInvariant();

        // Find the ABI array (root can be an array or an object with "abi":[...])
        JsonElement abiArray;
        using (var doc = JsonDocument.Parse(abiJson))
        {
            var root = doc.RootElement;
            if (root.ValueKind == JsonValueKind.Array)
            {
                abiArray = root;
            }
            else if (root.ValueKind == JsonValueKind.Object && root.TryGetProperty("abi", out var abiProp) && abiProp.ValueKind == JsonValueKind.Array)
            {
                abiArray = abiProp;
            }
            else
            {
                return (null, selector, null);
            }

            foreach (var el in abiArray.EnumerateArray())
            {
                if (el.ValueKind != JsonValueKind.Object) continue;
                if (!el.TryGetProperty("type", out var t)) continue;
                if (!t.ValueKind.Equals(JsonValueKind.String)) continue;
                if (!string.Equals(t.GetString(), "error", StringComparison.OrdinalIgnoreCase)) continue;

                // Build signature: ErrorName(type1,type2,...)
                if (!el.TryGetProperty("name", out var n) || n.ValueKind != JsonValueKind.String) continue;
                var name = n.GetString() ?? "";

                string[] inputTypes = Array.Empty<string>();
                if (el.TryGetProperty("inputs", out var inputs) && inputs.ValueKind == JsonValueKind.Array)
                {
                    inputTypes = inputs.EnumerateArray()
                        .Select(x => x.TryGetProperty("type", out var ty) && ty.ValueKind == JsonValueKind.String ? ty.GetString() ?? "" : "")
                        .ToArray();
                }

                var signature = $"{name}({string.Join(",", inputTypes)})";

                // keccak256(signature) -> take first 4 bytes
                var hash = Sha3Keccack.Current.CalculateHash(Encoding.UTF8.GetBytes(signature));
                var sigSel = BitConverter.ToString(hash, 0, 4).Replace("-", "").ToLowerInvariant();

                if (sigSel == selector)
                    return (name, selector, signature);
            }
        }

        // Not found in ABI, return selector so you can still log it
        return (null, selector, null);
    }
}