using HBRAK.Frontier.Api.Data.Chain.KillMail;
using HBRAK.Frontier.Api.Data.Chain.SmartAssemblies;
using HBRAK.Frontier.Api.Data.Chain.SmartCharacters;
using HBRAK.Frontier.Api.Data.Meta.Config;
using HBRAK.Frontier.Api.Data.Meta.ConfigAbi;
using HBRAK.Frontier.Api.Data.Meta.Health;
using HBRAK.Frontier.Authorization.Data;

namespace HBRAK.Frontier.Api.Service;

public interface IApiService
{
    public Task<T> GetListFromApiAsync<T>(string apiPath, AccessToken? accessToken = null, int limit = 10);
    public Task<T> GetFromApiAsync<T>(string apiPath, AccessToken? accessToken = null);
    public Task<HttpResponseMessage> GetHtppAsync(string url, AccessToken? accessToken = null);

    //meta
    public Task<ConfigAbiResponse> GetAbisConfigAsync();
    public Task<ConfigResponse> GetConfigAsync();
    public Task<HealthResponse> GetHealthAsync();

    //chain
    public Task<List<Killmail>> GetKillMailsAsync(int limit = 100);
    public Task<Killmail> GetKillMailIdAsync(string id);
    public Task<List<SmartAssemblyReference>> GetSmartAssembliesAsync(int limit = 100);
    public Task<SmartAssemblyBase> GetSmartAssemblyIdAsync(string id);
    public Task<List<SmartCharacterReference>> GetSmartCharactersAsync(int limit = 100);
    public Task<SmartCharacter> GetSmartCharacterIdAsync(string id);

    //game
    public Task<List<string>> GetFuelsAsync(int limit = 100);
    public Task<List<string>> GetSmartCharacterJumpsAsync(AccessToken accessToken, int limit = 100);
    public Task<List<string>> GetSmartCharacterJumpIdAsync(AccessToken accessToken);
    public Task<List<string>> GetSolarSystemsAsync(int limit = 100);
    public Task<List<string>> GetSolarSystemIdAsync(string id);
    public Task<List<string>> GetTribesAsync(int limit = 100);
    public Task<List<string>> GetTribeIdAsync(string id);
    public Task<List<string>> GetTypesAsync(int limit = 100);
    public Task<List<string>> GetTypeIdAsync(string id);

}
