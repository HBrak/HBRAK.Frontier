using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace HBRAK.Frontier.Communication.Api.Data.Meta.AbisConfig;

public class AbisConfigParameter
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("internalType")]
    public string? InternalType { get; set; }

    [JsonPropertyName("indexed")]
    public bool? Indexed { get; set; }

    [JsonPropertyName("components")]
    public List<AbisConfigParameter>? Components { get; set; }
}
