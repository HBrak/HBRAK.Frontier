namespace HBRAK.Frontier.Authorization.Service;

public interface ITokenStore
{
    Task SaveAsync<T>(T token, string applicationId, string account, CancellationToken ct = default) where T : class;
    Task<T?> LoadAsync<T>(string appId, string account, CancellationToken ct = default) where T : class; 
    Task<IReadOnlyList<T>> LoadAllAsync<T>(string appId, CancellationToken ct = default) where T : class;
    Task DeleteAsync(string appId, string account, CancellationToken ct = default);
}
