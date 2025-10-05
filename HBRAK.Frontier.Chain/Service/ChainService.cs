using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nethereum.Hex.HexConvertors.Extensions;
using Nethereum.Hex.HexTypes;
using Nethereum.JsonRpc.Client;
using Nethereum.RPC.Eth.DTOs;
using Nethereum.RPC.Web3;
using Nethereum.Web3;
using System;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;


namespace HBRAK.Frontier.Chain.Service;

public class ChainService : IChainService
{
    private readonly ILogger<ChainService> _logger;
    private readonly ChainServiceOptions _opts;
    private readonly Web3 _web3;

    public ChainService(ILogger<ChainService> logger, IOptions<ChainServiceOptions> options)
    {
        _logger = logger;
        _opts = options.Value;

        var rpc = new RpcClient(new Uri(_opts.RpcUrl));
        _web3 = new Web3(rpc);
    }

    // --- Node / chain ---
    public async Task<string?> GetClientVersionAsync(CancellationToken ct = default)
    {
        try
        {
            var req = new Web3ClientVersion(_web3.Client);
            return await req.SendRequestAsync();
        }
        catch (Exception ex) { _logger.LogWarning(ex, "GetClientVersion failed"); return null; }
    }

    public async Task<long?> GetChainIdAsync(CancellationToken ct = default)
    {
        try { var v = await _web3.Eth.ChainId.SendRequestAsync(); return (long)v.Value; }
        catch (Exception ex) { _logger.LogWarning(ex, "GetChainId failed"); return null; }
    }

    public async Task<ulong?> GetBlockNumberAsync(CancellationToken ct = default)
    {
        try
        {
            var v = await _web3.Eth.Blocks.GetBlockNumber.SendRequestAsync();
            return (ulong)v.Value;
        }
        catch (Exception ex) { _logger.LogWarning(ex, "GetBlockNumber failed"); return null; }
    }

    // --- Accounts ---
    public async Task<string?> GetBalanceHexAsync(string address, string blockTag = "latest", CancellationToken ct = default)
    {
        try
        {
            var wei = await _web3.Eth.GetBalance.SendRequestAsync(address, ToBlockParameter(blockTag));
            return "0x" + wei.Value.ToString("x");
        }
        catch (Exception ex) { _logger.LogWarning(ex, "GetBalanceHex failed"); return null; }
    }

    public async Task<decimal?> GetBalanceEthAsync(string address, string blockTag = "latest", CancellationToken ct = default)
    {
        try
        {
            var wei = await _web3.Eth.GetBalance.SendRequestAsync(address, ToBlockParameter(blockTag));
            return Nethereum.Util.UnitConversion.Convert.FromWei(wei);
        }
        catch (Exception ex) { _logger.LogWarning(ex, "GetBalanceEth failed"); return null; }
    }

    public async Task<ulong?> GetTransactionCountAsync(string address, string blockTag = "latest", CancellationToken ct = default)
    {
        try
        {
            var v = await _web3.Eth.Transactions.GetTransactionCount.SendRequestAsync(address, ToBlockParameter(blockTag));
            return (ulong)v.Value;
        }
        catch (Exception ex) { _logger.LogWarning(ex, "GetTransactionCount failed"); return null; }
    }

    // --- Read-only call ---
    public async Task<string?> CallAsync(object callInput, string blockTag = "latest", CancellationToken ct = default)
    {
        try
        {
            var tag = ToBlockParameter(blockTag);
            return await _web3.Eth.Transactions.Call.SendRequestAsync(ToCallInput(callInput), tag);
        }
        catch (Exception ex) { _logger.LogWarning(ex, "CallAsync failed"); return null; }
    }

    // --- Gas ---
    public async Task<string?> EstimateGasHexAsync(object txObject, CancellationToken ct = default)
    {
        try
        {
            var gas = await _web3.Eth.Transactions.EstimateGas.SendRequestAsync(ToCallInput(txObject));
            return "0x" + gas.Value.ToString("x");
        }
        catch (Exception ex) { _logger.LogWarning(ex, "EstimateGasHex failed"); return null; }
    }

    public async Task<ulong?> EstimateGasAsync(object txObject, CancellationToken ct = default)
    {
        try
        {
            var gas = await _web3.Eth.Transactions.EstimateGas.SendRequestAsync(ToCallInput(txObject));
            return (ulong)gas.Value;
        }
        catch (Exception ex) { _logger.LogWarning(ex, "EstimateGas failed"); return null; }
    }

    // --- Transactions ---
    public async Task<string?> SendRawTransactionAsync(string signedTxHex, CancellationToken ct = default)
    {
        try { return await _web3.Eth.Transactions.SendRawTransaction.SendRequestAsync(signedTxHex); }
        catch (Exception ex) { _logger.LogWarning(ex, "SendRawTransaction failed"); return null; }
    }

    public async Task<TransactionReceipt?> GetTransactionReceiptAsync(string txHash, CancellationToken ct = default)
    {
        try { return await _web3.Eth.Transactions.GetTransactionReceipt.SendRequestAsync(txHash); }
        catch (Exception ex) { _logger.LogWarning(ex, "GetTransactionReceipt failed"); return null; }
    }

    // --- helpers ---

    private static BlockParameter ToBlockParameter(string blockTag)
    {
        if (string.IsNullOrWhiteSpace(blockTag) || blockTag.Equals("latest", StringComparison.OrdinalIgnoreCase))
            return BlockParameter.CreateLatest();
        if (blockTag.Equals("pending", StringComparison.OrdinalIgnoreCase))
            return BlockParameter.CreatePending();
        if (blockTag.Equals("earliest", StringComparison.OrdinalIgnoreCase))
            return BlockParameter.CreateEarliest();

        // number (dec) or hex (0x..)
        if (blockTag.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            return new BlockParameter(new HexBigInteger(blockTag.HexToBigInteger(false)));

        if (ulong.TryParse(blockTag, out var dec))
            return new BlockParameter(new HexBigInteger(dec));

        // fallback
        return BlockParameter.CreateLatest();
    }

    private static CallInput ToCallInput(object tx)
    {
        var props = tx.GetType()
            .GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .ToDictionary(p => p.Name, p => p.GetValue(tx));

        string? Get(string n) => props.TryGetValue(n, out var v) ? v?.ToString() : null;

        static HexBigInteger? ToHexBig(string? hexOrDec)
        {
            if (string.IsNullOrWhiteSpace(hexOrDec)) return null;

            if (hexOrDec.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
                return new HexBigInteger(hexOrDec.HexToBigInteger(false));

            if (ulong.TryParse(hexOrDec, out var u))
                return new HexBigInteger(u);

            if (BigInteger.TryParse(hexOrDec, out var b))
                return new HexBigInteger(b);

            return null;
        }

        return new CallInput
        {
            From = Get("from"),
            To = Get("to"),
            Data = Get("data"),
            Value = ToHexBig(Get("value")),
            Gas = ToHexBig(Get("gas")),
            GasPrice = ToHexBig(Get("gasPrice")),
            MaxFeePerGas = ToHexBig(Get("maxFeePerGas")),
            MaxPriorityFeePerGas = ToHexBig(Get("maxPriorityFeePerGas"))
        };
    }
}