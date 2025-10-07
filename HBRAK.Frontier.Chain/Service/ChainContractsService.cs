using HBRAK.Frontier.Api.Data.Meta.AbisConfig;
using HBRAK.Frontier.Api.Service;
using Microsoft.Extensions.Logging;
using Nethereum.ABI.FunctionEncoding;
using Nethereum.ABI.Model;
using Nethereum.Contracts;
using Nethereum.Hex.HexConvertors.Extensions;
using Nethereum.Hex.HexTypes;
using Nethereum.Signer;
using Nethereum.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static Org.BouncyCastle.Math.EC.ECCurve;

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

        try
        {
            return await _chain.CallFunctionAsync<T>(config.DeployedTo, abiJson, functionName, fromAdress, args, blockTag, ct);
        }
        catch (SmartContractCustomErrorRevertException scEx)
        {
            var (name, sel, sig) = Tools.ErrorDecoder.TryDecodeName(abiJson, scEx.ExceptionEncodedData);

            if (!string.IsNullOrEmpty(name))
                _logger.LogWarning("Reverted {Function} with custom error {ErrorName} ({Signature}) [0x{Selector}] args={@Args}",
                    functionName, name, sig, sel, args);
            else
                _logger.LogWarning("Reverted {Function} with custom error selector 0x{Selector} (ABI has no matching error entry)",
                    functionName, sel);
            throw;
        }
    }

    public async Task<T?> SystemViewAsync<T>(
       string systemName,
       string functionName,
       object[]? args = null,
       string? fromAdress = null,
       string blockTag = "latest",
       CancellationToken ct = default)
    {
        await EnsureLoadedAsync(ct);

        var worldCfg = _cfg!.Cfg.FirstOrDefault(x =>
            string.Equals(x.Name, "IWorld", StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException("Contract IWorld not found.");
        var worldAbiJson = AsJson(worldCfg.Abi);

        var system = _cfg!.Systems.FirstOrDefault(x =>
            string.Equals(x.Name, systemName, StringComparison.OrdinalIgnoreCase) ||
            (!string.IsNullOrWhiteSpace(x.Label) &&
             string.Equals(x.Label, systemName, StringComparison.OrdinalIgnoreCase)))
            ?? throw new InvalidOperationException($"System '{systemName}' not found.");

        // Find the system function signature line
        var sigLine = system.Abi.FirstOrDefault(s =>
            s.StartsWith("function", StringComparison.OrdinalIgnoreCase) &&
            s.Contains($"{functionName}(", StringComparison.Ordinal))
            ?? throw new InvalidOperationException($"Function '{functionName}' not found in system '{systemName}'.");

        // Parse name, input types, output types
        var (parsedName, inputTypes, outputTypes) = ParseFunctionSignature(sigLine);
        functionName = parsedName;

        var inputParams = inputTypes.Select((t, i) => new Parameter(t, $"p{i}")).ToArray();
        var callArgs = NormalizeNumericArgs(inputParams, args ?? Array.Empty<object?>());

        // ---- Encode calldata: selector + encoded params (no function name text) ----
        var canonicalSig = $"{functionName}({string.Join(",", inputTypes)})";
        var selector = Sha3Keccack.Current.CalculateHash(canonicalSig).Substring(0, 8);

        var paramsEncoder = new ParametersEncoder();
        var encodedParams = paramsEncoder.EncodeParameters(inputParams, callArgs);
        var callDataHex = "0x" + selector + encodedParams.ToHex();
        var callDataBytes = callDataHex.HexToByteArray();
        var systemIdBytes = system.SystemId.HexToByteArray();

        // ---- Dispatch via IWorld ----
        byte[]? retBytes = null;
        try
        {
            // Prefer call(bytes32,bytes) -> bytes
            retBytes = await _chain.CallFunctionAsync<byte[]>(
                worldCfg.DeployedTo, worldAbiJson, "call",
                fromAdress, new object[] { systemIdBytes, callDataBytes }, blockTag, ct);
        }
        catch (SmartContractCustomErrorRevertException scEx)
        {
            var (name, sel, sig) = Tools.ErrorDecoder.TryDecodeName(worldAbiJson, scEx.ExceptionEncodedData);

            if (!string.IsNullOrEmpty(name))
                _logger.LogWarning("Reverted {Function} with custom error {ErrorName} ({Signature}) [0x{Selector}] args={@Args}",
                    functionName, name, sig, sel, args);
            else
                _logger.LogWarning("Reverted {Function} with custom error selector 0x{Selector} (ABI has no matching error entry)",
                    functionName, sel);
            throw;
        }

        if (retBytes is null) return default;

        // ---- Decode return (simple single value) ----
        var outHex = "0x" + retBytes.ToHex();
        var decoder = new FunctionCallDecoder();

        // Return type from ABI (preferred); else best-effort mapping from T
        var retType =
            outputTypes.FirstOrDefault()
            ?? InferSolTypeFromT(typeof(T))
            ?? throw new InvalidOperationException("Cannot determine return type for decode.");

        var outputParam = new Parameter(retType, "ret");
        return decoder.DecodeSimpleTypeOutput<T>(outputParam, outHex);
    }

    /* ------------ helpers ------------ */

    private static (string name, string[] inputTypes, string[] outputTypes) ParseFunctionSignature(string line)
    {
        var mName = Regex.Match(line, @"function\s+([A-Za-z_]\w*)\s*\(");
        if (!mName.Success) throw new InvalidOperationException($"Cannot parse function name from: {line}");
        var name = mName.Groups[1].Value;

        var open = line.IndexOf('(');
        var close = line.IndexOf(')', open + 1);
        if (open < 0 || close < 0 || close <= open) throw new InvalidOperationException($"Cannot parse inputs from: {line}");
        var inside = line.Substring(open + 1, close - open - 1).Trim();

        string[] inTypes = Array.Empty<string>();
        if (!string.IsNullOrWhiteSpace(inside))
        {
            inTypes = inside.Split(',')
                .Select(p => p.Trim())
                .Where(p => p.Length > 0)
                .Select(p => p.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries)[0])
                .ToArray();
        }

        string[] outTypes = Array.Empty<string>();
        var mRet = Regex.Match(line, @"returns\s*\(\s*([^)]+)\s*\)", RegexOptions.IgnoreCase);
        if (mRet.Success)
        {
            var retInside = mRet.Groups[1].Value;
            outTypes = retInside.Split(',')
                .Select(p => p.Trim())
                .Where(p => p.Length > 0)
                .Select(p => p.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries)[0])
                .ToArray();
        }

        return (name, inTypes, outTypes);
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


    // --- helpers ---

    // Best-effort mapping when ABI didn't specify returns(...)
    // Extend as needed if you hit more cases.
    private static string? InferSolTypeFromT(Type t)
    {
        if (t == typeof(bool)) return "bool";
        if (t == typeof(string)) return "string";
        if (t == typeof(byte[])) return "bytes";
        if (t == typeof(BigInteger)) return "uint256"; // common case
                                                       // add other primitives if you need them (e.g., "address", "bytes32", etc.)
        return null;
    }

    private static object[] NormalizeNumericArgs(Parameter[] pars, object[] provided)
    {
        var outArgs = new object[provided.Length];
        for (int i = 0; i < provided.Length; i++)
        {
            var p = i < pars.Length ? pars[i] : null;
            var t = p?.Type?.ToLowerInvariant() ?? "";
            if (!string.IsNullOrEmpty(t) && (t.StartsWith("uint") || t.StartsWith("int")))
            {
                var v = provided[i];
                if (v is Nethereum.Hex.HexTypes.HexBigInteger h) { outArgs[i] = h.Value; continue; }
                if (v is BigInteger) { outArgs[i] = v; continue; }
                if (v is string s)
                {
                    if (s.StartsWith("0x", StringComparison.OrdinalIgnoreCase)) { outArgs[i] = s.HexToBigInteger(false); continue; }
                    if (BigInteger.TryParse(s, out var bi)) { outArgs[i] = bi; continue; }
                }
                if (v is byte[] b) { outArgs[i] = new BigInteger(b, isUnsigned: true, isBigEndian: true); continue; }
            }
            outArgs[i] = provided[i];
        }
        return outArgs;
    }


    private static string AsJson(object? abi)
    {
        if (abi is null) return "[]";
        if (abi is string s) return s;
        if (abi is JsonElement je) return je.GetRawText();
        return JsonSerializer.Serialize(abi);
    }


}