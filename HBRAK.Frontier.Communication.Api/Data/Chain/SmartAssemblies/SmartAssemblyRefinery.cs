using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace HBRAK.Frontier.Communication.Api.Data.Chain.SmartAssemblies;

public class SmartAssemblyRefinery
{
    [JsonPropertyName("refinery")] public SimpleNodeInfo Refinery { get; set; } = new();
}
