﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace HBRAK.Frontier.Api.Data.Meta.ConfigAbi;

public class ConfigAbiCfg
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("chain_id")]
    public long ChainId { get; set; }

    [JsonPropertyName("urls")]
    public RpcUrls Urls { get; set; } = new();

    [JsonPropertyName("deployed_to")]
    public string DeployedTo { get; set; } = string.Empty;

    [JsonPropertyName("abi")]
    public List<ConfigAbiEntry> Abi { get; set; } = new();

    [JsonPropertyName("eip712")]
    public ConfigAbiEip712Descriptor Eip712 { get; set; } = new();
}
