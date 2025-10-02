using HBRAK.Frontier.Api.Data;
using HBRAK.Frontier.Api.Data.Chain;
using HBRAK.Frontier.Api.Data.Chain.Enums;
using HBRAK.Frontier.Api.Data.Chain.KillMail;
using HBRAK.Frontier.Api.Data.Chain.SmartAssemblies;
using HBRAK.Frontier.Api.Data.Chain.SmartCharacters;
using HBRAK.Frontier.Api.Data.Game.Fuels;
using HBRAK.Frontier.Api.Data.Game.Jumps;
using HBRAK.Frontier.Api.Data.Game.SolarSystems;
using HBRAK.Frontier.Api.Data.Game.Tribes;
using HBRAK.Frontier.Api.Data.Game.Type;
using HBRAK.Frontier.Api.Data.Meta.Config;
using HBRAK.Frontier.Api.Data.Meta.ConfigAbi;
using HBRAK.Frontier.Api.Data.Meta.Health;
using HBRAK.Frontier.Authorization.Data;

namespace HBRAK.Frontier.Api.Service;

public interface IApiService
{
    public Task<List<T>> GetListFromApiAsync<T>(string apiPath, AccessToken? accessToken = null, int limit = 10) where T : class;
    public Task<T?> GetFromApiAsync<T>(string apiPath, AccessToken? accessToken = null) where T : class;

    //meta
    public Task<ConfigAbiResponse?> GetAbisConfigAsync();
    public Task<List<ConfigResponse>?> GetConfigAsync(); // yeah i dont know.. it just sends in array..
    public Task<HealthResponse?> GetHealthAsync();

    //chain
    public Task<List<Killmail>> GetKillMailsAsync(int limit = 100);
    public Task<Killmail?> GetKillMailIdAsync(string id);
    public Task<List<SmartAssemblyReference>> GetSmartAssembliesAsync(SmartAssemblyType? type = null, int limit = 100);
    public Task<SmartAssemblyBase?> GetSmartAssemblyIdAsync(string id);
    public Task<List<SmartCharacterReference>> GetSmartCharactersAsync(int limit = 100);
    public Task<SmartCharacter?> GetSmartCharacterIdAsync(string id);

    //game
    public Task<List<FuelType>> GetFuelsAsync(int limit = 100);
    public Task<List<SmartCharacterJump>> GetSmartCharacterJumpsAsync(AccessToken accessToken, int limit = 100);
    public Task<SmartCharacterJump?> GetSmartCharacterJumpIdAsync(string id, AccessToken accessToken);
    public Task<List<SolarSystemReference>> GetSolarSystemsAsync(int limit = 100);
    public Task<SolarSystem?> GetSolarSystemIdAsync(string id);
    public Task<List<TribeReference>> GetTribesAsync(int limit = 100);
    public Task<Tribe?> GetTribeIdAsync(string id);
    public Task<List<TypeDetails>> GetTypesAsync(int limit = 100);
    public Task<TypeDetails?> GetTypeIdAsync(string id);

}
