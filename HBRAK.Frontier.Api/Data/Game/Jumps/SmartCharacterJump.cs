using HBRAK.Frontier.Api.Data.Game.SolarSystems;
using HBRAK.Frontier.Api.Data.Info;
using System.Text.Json.Serialization;

namespace HBRAK.Frontier.Api.Data.Game.Jumps;

public class SmartCharacterJump
{
    [JsonPropertyName("destination")]
    public SolarSystemReference Destination { get; set; } = new();

    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("origin")]
    public SolarSystemReference Origin { get; set; } = new();

    [JsonPropertyName("ship")]
    public ShipInfo Ship { get; set; } = new();

    [JsonPropertyName("time")]
    public DateTimeOffset Time { get; set; }
}
