using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace HBRAK.Frontier.Api.Data.Meta.Config;

public class ConfigContracts
{
    [JsonPropertyName("contractsVersion")]
    public string ContractsVersion { get; set; } = string.Empty;

    [JsonPropertyName("eveToken")]
    public ConfigContractAccess EveToken { get; set; } = new();

    [JsonPropertyName("forwarder")]
    public ConfigContractAccess Forwarder { get; set; } = new();

    [JsonPropertyName("lensSeller")]
    public ConfigContractAccess LensSeller { get; set; } = new();

    [JsonPropertyName("world")]
    public ConfigContractAccess World { get; set; } = new();
}
