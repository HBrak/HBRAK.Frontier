using HBRAK.Frontier.Api.Data.Chain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace HBRAK.Frontier.Api.Data.Chain.SmartAssemblies;

[JsonConverter(typeof(SmartAssemblyConverter))]
public abstract class SmartAssemblyBase
{
    [JsonPropertyName("dappURL")] public string DappUrl { get; set; } = string.Empty;
    [JsonPropertyName("description")] public string Description { get; set; } = string.Empty;
    [JsonPropertyName("energyUsage")] public int EnergyUsage { get; set; }
    [JsonPropertyName("id")] public string Id { get; set; } = string.Empty;
    [JsonPropertyName("location")] public WorldLocation Location { get; set; } = new();
    [JsonPropertyName("name")] public string Name { get; set; } = string.Empty;
    [JsonPropertyName("owner")] public CharacterRef Owner { get; set; } = new();
    [JsonPropertyName("solarSystem")] public SolarSystemReference SolarSystem { get; set; } = new();
    [JsonPropertyName("state")] public string State { get; set; } = string.Empty;
    [JsonPropertyName("type")] public SmartAssemblyType Type { get; set; } = SmartAssemblyType.Unknown;
    [JsonPropertyName("typeDetails")] public TypeDetails TypeDetails { get; set; } = new();
    [JsonPropertyName("typeId")] public int TypeId { get; set; }
}

