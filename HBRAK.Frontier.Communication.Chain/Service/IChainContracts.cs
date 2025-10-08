namespace HBRAK.Frontier.Communication.Chain.Service;

public interface IChainContracts
{
    Task<T?> ContractViewAsync<T>(string contractName, string functionName,
                                  object[]? args = null, string? fromAdress = null,
                                  string blockTag = "latest", CancellationToken ct = default);
    Task<T?> SystemViewAsync<T>(string systemName, string functionName,
                                           object[]? args = null, string? fromAdress = null,
                                           string blockTag = "latest", CancellationToken ct = default);
    Task<string?> GetSystemIdAsync(string opKey, CancellationToken ct = default);
}
