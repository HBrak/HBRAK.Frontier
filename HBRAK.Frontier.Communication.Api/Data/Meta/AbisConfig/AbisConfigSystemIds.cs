using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace HBRAK.Frontier.Communication.Api.Data.Meta.AbisConfig;

public class AbisConfigSystemIds
{
    [JsonPropertyName("createCharacter")]
    public string CreateCharacter { get; set; } = string.Empty;

    [JsonPropertyName("createAndAnchorSmartStorageUnit")]
    public string CreateAndAnchorSmartStorageUnit { get; set; } = string.Empty;

    [JsonPropertyName("destroyDeployable")]
    public string DestroyDeployable { get; set; } = string.Empty;

    [JsonPropertyName("unanchor")]
    public string Unanchor { get; set; } = string.Empty;

    [JsonPropertyName("bringOnline")]
    public string BringOnline { get; set; } = string.Empty;

    [JsonPropertyName("bringOffline")]
    public string BringOffline { get; set; } = string.Empty;

    [JsonPropertyName("createAndDepositItemsToInventory")]
    public string CreateAndDepositItemsToInventory { get; set; } = string.Empty;

    [JsonPropertyName("createAndDepositItemsToEphemeralInventory")]
    public string CreateAndDepositItemsToEphemeralInventory { get; set; } = string.Empty;

    [JsonPropertyName("withdrawFromInventory")]
    public string WithdrawFromInventory { get; set; } = string.Empty;

    [JsonPropertyName("withdrawFromEphemeralInventory")]
    public string WithdrawFromEphemeralInventory { get; set; } = string.Empty;

    [JsonPropertyName("depositFuel")]
    public string DepositFuel { get; set; } = string.Empty;

    [JsonPropertyName("withdrawFuel")]
    public string WithdrawFuel { get; set; } = string.Empty;

    [JsonPropertyName("updateFuel")]
    public string UpdateFuel { get; set; } = string.Empty;

    [JsonPropertyName("depositToSSU")]
    public string DepositToSSU { get; set; } = string.Empty;

    [JsonPropertyName("purchaseItem")]
    public string PurchaseItem { get; set; } = string.Empty;

    [JsonPropertyName("approveEVE")]
    public string ApproveEVE { get; set; } = string.Empty;

    [JsonPropertyName("createAndAnchorSmartTurret")]
    public string CreateAndAnchorSmartTurret { get; set; } = string.Empty;

    [JsonPropertyName("createAndAnchorSmartGate")]
    public string CreateAndAnchorSmartGate { get; set; } = string.Empty;

    [JsonPropertyName("reportKill")]
    public string ReportKill { get; set; } = string.Empty;

    [JsonPropertyName("transfer")]
    public string Transfer { get; set; } = string.Empty;
}
