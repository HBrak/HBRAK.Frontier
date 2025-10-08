using HBRAK.Frontier.Communication.Api.Data.Meta;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace HBRAK.Frontier.Communication.Api.Data.Meta.AbisConfig;

public class AbisConfigCfg
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
    public List<AbisConfigEntry> Abi { get; set; } = new();

    [JsonPropertyName("eip712")]
    public AbisConfigEip712Descriptor Eip712 { get; set; } = new();
}
