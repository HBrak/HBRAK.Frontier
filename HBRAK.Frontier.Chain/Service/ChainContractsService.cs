using HBRAK.Frontier.Api.Data.Meta.AbisConfig;
using HBRAK.Frontier.Api.Service;
using Microsoft.Extensions.Logging;
using Nethereum.Hex.HexConvertors.Extensions;
using Nethereum.Hex.HexTypes;
using Nethereum.Signer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace HBRAK.Frontier.Chain.Service;

public class ChainContractsService : IChainContracts
{
    public ChainContractsService(IApiService api, IChainService chain, ILogger<ChainContractsService> logger)
    {
        _api = api ?? throw new ArgumentNullException(nameof(api));
        _chain = chain ?? throw new ArgumentNullException(nameof(chain));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    private IApiService _api;
    private IChainService _chain;
    private ILogger<ChainContractsService> _logger;
    private AbisConfigResponse? _cfg;

    private async Task EnsureLoadedAsync(CancellationToken ct)
    {
        if (_cfg is not null) return;
        _cfg = await _api.GetAbisConfigAsync()
               ?? throw new InvalidOperationException("ABIs config not available.");
    }

    public async Task<T?> ContractViewAsync<T>(string contractName, string functionName,
                                               object[]? args = null, string? fromAdress = null,
                                               string blockTag = "latest", CancellationToken ct = default)
    {
        await EnsureLoadedAsync(ct);

        var config = _cfg!.Cfg.FirstOrDefault(x =>
            string.Equals(x.Name, contractName, StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException($"Contract '{contractName}' not found.");

        var function = config.Abi.FirstOrDefault(function => function.Name == functionName)
            ?? throw new InvalidOperationException($"Function '{functionName}' not found in contract {contractName}.");

        var abiJson = AsJson(config.Abi);
        return await _chain.CallFunctionAsync<T>(config.DeployedTo, abiJson, functionName, fromAdress, args, blockTag, ct);
    }

    public async Task<string?> GetSystemIdAsync(string operationKey, CancellationToken ct = default)
    {
        await EnsureLoadedAsync(ct);

        var idsObj = _cfg!.SystemIds;
        if (idsObj is null) return null;

        var prop = idsObj.GetType()
            .GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .FirstOrDefault(p => string.Equals(p.Name, operationKey, StringComparison.OrdinalIgnoreCase));

        if (prop is null) return null;

        var raw = prop.GetValue(idsObj);
        return raw switch
        {
            null => null,
            string s => s,
            JsonElement je => je.ValueKind == JsonValueKind.String ? je.GetString() : je.GetRawText(),
            _ => raw.ToString()
        };
    }
    private static string AsJson(object? abi)
    {
        if (abi is null) return "[]";
        if (abi is string s) return s;
        if (abi is JsonElement je) return je.GetRawText();
        return JsonSerializer.Serialize(abi);
    }


}