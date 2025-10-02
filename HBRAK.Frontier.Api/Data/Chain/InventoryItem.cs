using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace HBRAK.Frontier.Api.Data.Chain;

public class InventoryItem
{
    [JsonPropertyName("image")] public string Image { get; set; } = string.Empty;
    [JsonPropertyName("itemId")] public int ItemId { get; set; }
    [JsonPropertyName("name")] public string Name { get; set; } = string.Empty;
    [JsonPropertyName("quantity")] public int Quantity { get; set; }
    [JsonPropertyName("typeId")] public int TypeId { get; set; }
}
