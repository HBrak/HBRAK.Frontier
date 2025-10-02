using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace HBRAK.Frontier.Api.Data.Info;

public sealed class StorageInfo
{
    [JsonPropertyName("ephemeralInventories")] public List<EphemeralInventory> EphemeralInventories { get; set; } = new();
    [JsonPropertyName("isParentNodeOnline")] public bool IsParentNodeOnline { get; set; }
    [JsonPropertyName("mainInventory")] public Inventory MainInventory { get; set; } = new();
}
