using HBRAK.Frontier.Api.Data.Chain.SmartAssemblies;
using HBRAK.Frontier.Api.Data.Chain.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace HBRAK.Frontier.Api.Data.Chain.Enums;

[JsonConverter(typeof(SmartAssemblyTypeConverter))]
public enum SmartAssemblyType
{
    Unknown = 0,
    SmartStorageUnit,
    SmartGate,
    SmartTurret,
    Manufacturing,
    Refinery,
    NetworkNode,
    SmartHangar
}

