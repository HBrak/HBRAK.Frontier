using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace HBRAK.Frontier.Api.Data.Chain;

public class SolarSystemReference
{
    [JsonPropertyName("constellationId")] public int ConstellationId { get; set; }
    [JsonPropertyName("id")] public int Id { get; set; }
    [JsonPropertyName("location")] public WorldLocation Location { get; set; } = new();
    [JsonPropertyName("name")] public string Name { get; set; } = string.Empty;
    [JsonPropertyName("regionId")] public int RegionId { get; set; }
}
