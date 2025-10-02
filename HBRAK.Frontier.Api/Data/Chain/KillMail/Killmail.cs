using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace HBRAK.Frontier.Api.Data.Chain.KillMail;

public class Killmail
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("killer")]
    public SmartCharacterReference Killer { get; set; } = new();

    [JsonPropertyName("solarSystemId")]
    public int SolarSystemId { get; set; }

    [JsonPropertyName("time")]
    public string Time { get; set; } = string.Empty;

    [JsonPropertyName("victim")]
    public SmartCharacterReference Victim { get; set; } = new();
}
