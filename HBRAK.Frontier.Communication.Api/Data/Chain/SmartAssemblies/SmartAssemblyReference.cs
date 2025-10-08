using HBRAK.Frontier.Communication.Api.Data.Chain.SmartCharacters;
using HBRAK.Frontier.Communication.Api.Data.Game.SolarSystems;
using HBRAK.Frontier.Communication.Api.Data.Game.Type;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace HBRAK.Frontier.Communication.Api.Data.Chain.SmartAssemblies;

public class SmartAssemblyReference
{
    [JsonPropertyName("energyUsage")] public int EnergyUsage { get; set; }
    [JsonPropertyName("id")] public string Id { get; set; } = string.Empty;
    [JsonPropertyName("name")] public string Name { get; set; } = string.Empty;
    [JsonPropertyName("owner")] public SmartCharacterReference Owner { get; set; } = new();
    [JsonPropertyName("solarSystem")] public SolarSystemReference SolarSystem { get; set; } = new();
    [JsonPropertyName("state")] public string State { get; set; } = string.Empty;
    [JsonPropertyName("type")] public string Type { get; set; } = string.Empty;
    [JsonPropertyName("typeDetails")] public TypeDetails TypeDetails { get; set; } = new();
    [JsonPropertyName("typeId")] public int TypeId { get; set; }
}
