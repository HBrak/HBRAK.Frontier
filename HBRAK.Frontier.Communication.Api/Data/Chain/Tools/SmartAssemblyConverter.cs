using HBRAK.Frontier.Communication.Api.Data.Chain.Enums;
using HBRAK.Frontier.Communication.Api.Data.Chain.SmartAssemblies;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace HBRAK.Frontier.Communication.Api.Data.Chain.Tools;

internal class SmartAssemblyConverter : JsonConverter<SmartAssemblyBase>
{
    public override SmartAssemblyBase? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var doc = JsonDocument.ParseValue(ref reader);
        var root = doc.RootElement;

        var kind = SmartAssemblyType.Unknown;
        if (root.TryGetProperty("type", out var t) && t.ValueKind == JsonValueKind.String)
        {
            var s = t.GetString();
            kind = s switch
            {
                "SmartStorageUnit" => SmartAssemblyType.SmartStorageUnit,
                "SmartGate" => SmartAssemblyType.SmartGate,
                "SmartTurret" => SmartAssemblyType.SmartTurret,
                "Manufacturing" => SmartAssemblyType.Manufacturing,
                "Refinery" => SmartAssemblyType.Refinery,
                "NetworkNode" => SmartAssemblyType.NetworkNode,
                "SmartHangar" => SmartAssemblyType.SmartHangar,
                _ => SmartAssemblyType.Unknown
            };

            if (kind == SmartAssemblyType.Unknown)
            {
                Console.WriteLine($"{s} : SmartAssembly type unknown");
                return null;
            }
                
        }else
        {
            // type property missing
            Console.WriteLine("SmartAssembly type property missing");
            return null;
        }

        var json = root.GetRawText();
        var targetType = kind switch
        {
            SmartAssemblyType.SmartStorageUnit => typeof(SmartAssemblyStorageUnit),
            SmartAssemblyType.SmartGate => typeof(SmartAssemblyGate),
            SmartAssemblyType.SmartTurret => typeof(SmartTurretAssembly),
            SmartAssemblyType.Manufacturing => typeof(SmartAssemblyManufacturing),
            SmartAssemblyType.Refinery => typeof(SmartAssemblyRefinery),
            SmartAssemblyType.NetworkNode => typeof(SmartAssemblyNetworkNode),
            SmartAssemblyType.SmartHangar => typeof(SmartAssemblyHangar),
            _ => typeof(SmartAssemblyUnknown)
        };

        var result = (SmartAssemblyBase?)JsonSerializer.Deserialize(json, targetType, options);
        // Ensure the enum is set (if the converter for enum mapped "unknown")
        if (result != null && result.Type == SmartAssemblyType.Unknown && root.TryGetProperty("type", out var raw))
        {
            // reuse the enum converter logic
            try
            {
                result.Type = JsonSerializer.Deserialize<SmartAssemblyType>(raw.GetRawText(), options);
            }
            catch { /* ignore */ }
        }
        return result;
    }

    public override void Write(Utf8JsonWriter writer, SmartAssemblyBase value, JsonSerializerOptions options)
    {
        // Serialize using the runtime type so the correct module properties are written
        JsonSerializer.Serialize(writer, value, value.GetType(), options);
    }
}
