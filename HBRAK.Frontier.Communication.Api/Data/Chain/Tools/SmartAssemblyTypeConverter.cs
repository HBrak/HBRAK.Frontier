using HBRAK.Frontier.Communication.Api.Data.Chain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace HBRAK.Frontier.Communication.Api.Data.Chain.Tools;

internal sealed class SmartAssemblyTypeConverter : JsonConverter<SmartAssemblyType>
{
    public override SmartAssemblyType Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var s = reader.GetString();
        return s switch
        {
            "SmartStorageUnit" => SmartAssemblyType.SmartStorageUnit,
            "SmartGate" => SmartAssemblyType.SmartGate,
            "SmartTurret" => SmartAssemblyType.SmartTurret,
            "Manufacturing" => SmartAssemblyType.Manufacturing,
            "Refinery" => SmartAssemblyType.Refinery,
            _ => SmartAssemblyType.Unknown
        };
    }

    public override void Write(Utf8JsonWriter writer, SmartAssemblyType value, JsonSerializerOptions options)
    {
        var s = value switch
        {
            SmartAssemblyType.SmartStorageUnit => "SmartStorageUnit",
            SmartAssemblyType.SmartGate => "SmartGate",
            SmartAssemblyType.SmartTurret => "SmartTurret",
            SmartAssemblyType.Manufacturing => "Manufacturing",
            SmartAssemblyType.Refinery => "Refinery",
            _ => "unknown"
        };
        writer.WriteStringValue(s);
    }
}
