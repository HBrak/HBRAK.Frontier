using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace HBRAK.Frontier.Communication.Api.Data.Meta.Config;

public class ConfigResponse
{
    [JsonPropertyName("EVEToLuxExchangeRate")]
    public int EveToLuxExchangeRate { get; set; }

    [JsonPropertyName("baseDappUrl")]
    public string BaseDappUrl { get; set; } = string.Empty;

    [JsonPropertyName("blockExplorerUrl")]
    public string BlockExplorerUrl { get; set; } = string.Empty;

    [JsonPropertyName("chainId")]
    public int ChainId { get; set; }

    [JsonPropertyName("contracts")]
    public ConfigContracts Contracts { get; set; } = new();

    [JsonPropertyName("cycleStartDate")]
    public string CycleStartDate { get; set; } = string.Empty;

    [JsonPropertyName("exchangeWalletAddress")]
    public string ExchangeWalletAddress { get; set; } = string.Empty;

    [JsonPropertyName("indexerUrl")]
    public string IndexerUrl { get; set; } = string.Empty;

    [JsonPropertyName("ipfsApiUrl")]
    public string IpfsApiUrl { get; set; } = string.Empty;

    [JsonPropertyName("itemTypeIDs")]
    public ConfigItemTypeIDs ItemTypeIDs { get; set; } = new();

    [JsonPropertyName("metadataApiUrl")]
    public string MetadataApiUrl { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("nativeCurrency")]
    public ConfigNativeCurrency NativeCurrency { get; set; } = new();

    [JsonPropertyName("podPublicSigningKey")]
    public string PodPublicSigningKey { get; set; } = string.Empty;

    [JsonPropertyName("rpcUrls")]
    public ConfigRpcEndpoint RpcUrls { get; set; } = new();

    [JsonPropertyName("systems")]
    public ConfigSystems Systems { get; set; } = new();

    [JsonPropertyName("vaultDappUrl")]
    public string VaultDappUrl { get; set; } = string.Empty;

    [JsonPropertyName("walletApiUrl")]
    public string WalletApiUrl { get; set; } = string.Empty;
}


