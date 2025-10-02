using HBRAK.Frontier.Api.Data.Game.Type;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace HBRAK.Frontier.Api.Data.Game.Fuels;

public class FuelType
{
    [JsonPropertyName("efficiency")]
    public int Efficiency { get; set; }

    [JsonPropertyName("type")]
    public TypeDetails Type { get; set; } = new();
}
