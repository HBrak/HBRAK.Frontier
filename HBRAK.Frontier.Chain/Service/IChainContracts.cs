using HBRAK.Frontier.Authorization.Data;
using Nethereum.Hex.HexTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace HBRAK.Frontier.Chain.Service;

public interface IChainContracts
{
    Task<T?> ContractViewAsync<T>(string contractName, string functionName,
                                  object[]? args = null, string? fromAdress = null,
                                  string blockTag = "latest", CancellationToken ct = default);
    Task<string?> GetSystemIdAsync(string opKey, CancellationToken ct = default);
}
