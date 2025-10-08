using HBRAK.Frontier.Communication.Api.Data.Chain.SmartAssemblies;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace HBRAK.Frontier.Communication.Api.Data.Chain.SmartCharacters;

public class SmartCharacter
{
    [JsonPropertyName("address")]
    public string Address { get; set; } = string.Empty;

    [JsonPropertyName("eveBalanceInWei")]
    public string EveBalanceInWei { get; set; } = string.Empty;

    [JsonPropertyName("gasBalanceInWei")]
    public string GasBalanceInWei { get; set; } = string.Empty;

    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("portraitUrl")]
    public string PortraitUrl { get; set; } = string.Empty;

    [JsonPropertyName("smartAssemblies")]
    public List<SmartAssemblyReference> SmartAssemblies { get; set; } = new();

    [JsonPropertyName("tribeId")]
    public int TribeId { get; set; }
}
