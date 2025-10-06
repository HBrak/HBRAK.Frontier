using Nethereum.Hex.HexTypes;
using Nethereum.RPC.Eth.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HBRAK.Frontier.Chain.Service;

public interface IChainService
{
    Task<T?> CallFunctionAsync<T>(
        string contractAddress,
        string abiJson,
        string functionName,
        string? fromAddress = null,
        object[]? args = null,
        string blockTag = "latest",
        CancellationToken ct = default
     );

    // Node / chain
    Task<string?> GetClientVersionAsync(CancellationToken ct = default);
    Task<long?> GetChainIdAsync(CancellationToken ct = default);
    Task<ulong?> GetBlockNumberAsync(CancellationToken ct = default);

    // Accounts
    Task<string?> GetBalanceHexAsync(string address, string blockTag = "latest", CancellationToken ct = default);
    Task<decimal?> GetBalanceEthAsync(string address, string blockTag = "latest", CancellationToken ct = default);
    Task<ulong?> GetTransactionCountAsync(string address, string blockTag = "latest", CancellationToken ct = default);

    // Read-only call
    Task<string?> CallAsync(object callInput, string blockTag = "latest", CancellationToken ct = default);

    // Gas
    Task<string?> EstimateGasHexAsync(object txObject, CancellationToken ct = default);
    Task<ulong?> EstimateGasAsync(object txObject, CancellationToken ct = default);

    // Transactions
    Task<string?> SendRawTransactionAsync(string signedTxHex, CancellationToken ct = default);
    Task<TransactionReceipt?> GetTransactionReceiptAsync(string txHash, CancellationToken ct = default);
}
