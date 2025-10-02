using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace HBRAK.Frontier.Api.Data.Meta.ConfigAbi;

public class ConfigAbiEntry
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("inputs")]
    public List<ConfigAbiParameter>? Inputs { get; set; }

    [JsonPropertyName("outputs")]
    public List<ConfigAbiParameter>? Outputs { get; set; }

    [JsonPropertyName("stateMutability")]
    public string? StateMutability { get; set; }

    [JsonPropertyName("anonymous")]
    public bool? Anonymous { get; set; }
}
