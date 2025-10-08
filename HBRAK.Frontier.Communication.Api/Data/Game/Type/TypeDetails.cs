using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace HBRAK.Frontier.Communication.Api.Data.Game.Type;

public class TypeDetails
{
    [JsonPropertyName("categoryId")] public int CategoryId { get; set; }
    [JsonPropertyName("categoryName")] public string CategoryName { get; set; } = string.Empty;
    [JsonPropertyName("description")] public string Description { get; set; } = string.Empty;
    [JsonPropertyName("groupId")] public int GroupId { get; set; }
    [JsonPropertyName("groupName")] public string GroupName { get; set; } = string.Empty;
    [JsonPropertyName("iconUrl")] public string IconUrl { get; set; } = string.Empty;
    [JsonPropertyName("id")] public int Id { get; set; }
    [JsonPropertyName("mass")] public double Mass { get; set; }
    [JsonPropertyName("name")] public string Name { get; set; } = string.Empty;
    [JsonPropertyName("portionSize")] public int PortionSize { get; set; }
    [JsonPropertyName("radius")] public double Radius { get; set; }
    [JsonPropertyName("volume")] public double Volume { get; set; }
}
