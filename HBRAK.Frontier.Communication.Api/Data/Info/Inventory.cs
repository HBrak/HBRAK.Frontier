using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace HBRAK.Frontier.Communication.Api.Data.Info;

public class Inventory
{
    [JsonPropertyName("capacity")] public string Capacity { get; set; } = string.Empty;
    [JsonPropertyName("items")] public List<InventoryItem> Items { get; set; } = new();
    [JsonPropertyName("usedCapacity")] public string UsedCapacity { get; set; } = string.Empty;
}