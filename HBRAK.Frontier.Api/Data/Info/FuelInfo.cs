using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace HBRAK.Frontier.Api.Data.Info;

public class FuelInfo
{
    [JsonPropertyName("amount")] public int Amount { get; set; }
    [JsonPropertyName("burnRateInSec")] public int BurnRateInSec { get; set; }
    [JsonPropertyName("efficiency")] public int Efficiency { get; set; }
    [JsonPropertyName("typeId")] public int TypeId { get; set; }
    [JsonPropertyName("unitVolume")] public long UnitVolume { get; set; }
}