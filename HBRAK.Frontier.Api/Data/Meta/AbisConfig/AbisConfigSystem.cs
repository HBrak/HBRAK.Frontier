using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace HBRAK.Frontier.Api.Data.Meta.AbisConfig;

public class AbisConfigSystem
{
    [JsonPropertyName("namespaceLabel")]
    public string NamespaceLabel { get; set; } = string.Empty;

    [JsonPropertyName("label")]
    public string Label { get; set; } = string.Empty;

    [JsonPropertyName("namespace")]
    public string NamespaceValue { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("systemId")]
    public string SystemId { get; set; } = string.Empty;

    [JsonPropertyName("abi")]
    public List<string> Abi { get; set; } = new();

    [JsonPropertyName("worldAbi")]
    public List<string> WorldAbi { get; set; } = new();
}
