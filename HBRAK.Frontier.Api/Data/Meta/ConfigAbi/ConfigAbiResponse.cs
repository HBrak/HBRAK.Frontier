﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace HBRAK.Frontier.Api.Data.Meta.ConfigAbi;

public class ConfigAbiResponse
{
    [JsonPropertyName("system_ids")]
    public ConfigAbiSystemIds SystemIds { get; set; } = new();

    [JsonPropertyName("vault_dapp_url")]
    public string VaultDappUrl { get; set; } = string.Empty;

    [JsonPropertyName("base_dapp_url")]
    public string BaseDappUrl { get; set; } = string.Empty;

    [JsonPropertyName("exchange_wallet_address")]
    public string ExchangeWalletAddress { get; set; } = string.Empty;

    [JsonPropertyName("EVE_to_LUX_exchange_rate")]
    public int EveToLuxExchangeRate { get; set; }

    [JsonPropertyName("cfg")]
    public List<ConfigAbiCfg> Cfg { get; set; } = new();

    [JsonPropertyName("systems")]
    public List<ConfigAbiSystem> Systems { get; set; } = new();
}
