using HBRAK.Frontier.Communication.Api.Data.Chain.SmartAssemblies;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace HBRAK.Frontier.Communication.Api.Data.Game.SolarSystems;

public class SolarSystem : SolarSystemReference
{
    [JsonPropertyName("smartAssemblies")]
    public List<SmartAssemblyReference> SmartAssemblies { get; set; } = new();
}
