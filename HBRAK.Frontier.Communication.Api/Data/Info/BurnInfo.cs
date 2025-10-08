using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace HBRAK.Frontier.Communication.Api.Data.Info;

public class BurnInfo
{
    [JsonPropertyName("isBurning")] public bool IsBurning { get; set; }
    [JsonPropertyName("previousCycleElapsedTimeInSec")] public int PreviousCycleElapsedTimeInSec { get; set; }
    [JsonPropertyName("startTime")] public string StartTime { get; set; } = string.Empty;
}