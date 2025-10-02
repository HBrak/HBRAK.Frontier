using HBRAK.Frontier.Api.Data.Info;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace HBRAK.Frontier.Api.Data.Chain.SmartAssemblies;

public class NetworkNodeInfo
{
    [JsonPropertyName("burn")] public BurnInfo Burn { get; set; } = new();
    [JsonPropertyName("energyMaxCapacity")] public int EnergyMaxCapacity { get; set; }
    [JsonPropertyName("energyProduction")] public int EnergyProduction { get; set; }
    [JsonPropertyName("fuel")] public FuelInfo Fuel { get; set; } = new();
    [JsonPropertyName("linkedAssemblies")] public List<SmartAssemblyReference> LinkedAssemblies { get; set; } = new();
    [JsonPropertyName("totalReservedEnergy")] public int TotalReservedEnergy { get; set; }
}
