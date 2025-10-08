using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace HBRAK.Frontier.Communication.Api.Data.Game.Tribes;

public class TribeReference
{
    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("foundedAt")]
    public DateTimeOffset FoundedAt { get; set; }

    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("memberCount")]
    public int MemberCount { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("nameShort")]
    public string NameShort { get; set; } = string.Empty;

    [JsonPropertyName("taxRate")]
    public double TaxRate { get; set; }

    [JsonPropertyName("tribeUrl")]
    public string TribeUrl { get; set; } = string.Empty;
}
