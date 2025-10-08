using HBRAK.Frontier.Communication.Api.Data.Chain;
using HBRAK.Frontier.Authorization.Api.Data;
using HBRAK.Frontier.Communication.Api.Data.Chain.SmartCharacters;
using HBRAK.Frontier.Communication.Api.Data.Game.Tribes;
using HBRAK.Frontier.Communication.Api.Data.Game.SolarSystems;
using HBRAK.Frontier.Communication.Api.Data.Game.Jumps;
using HBRAK.Frontier.Communication.Api.Data.Chain.Enums;
using HBRAK.Frontier.Communication.Api.Data.Meta.Config;
using HBRAK.Frontier.Communication.Api.Data.Chain.KillMail;
using HBRAK.Frontier.Communication.Api.Data.Meta.Health;
using HBRAK.Frontier.Communication.Api.Data.Game.Fuels;
using HBRAK.Frontier.Communication.Api.Data.Game.Type;
using HBRAK.Frontier.Communication.Api.Data.Chain.SmartAssemblies;
using HBRAK.Frontier.Communication.Api.Data.Meta.AbisConfig;

namespace HBRAK.Frontier.Communication.Api.Service;

public interface IApiService
{
    public Task<List<T>> GetListFromApiAsync<T>(string apiPath, ApiToken? accessToken = null, int? limit = null, Dictionary<string, string>? extraParams = null) where T : class?;
    public Task<T?> GetFromApiAsync<T>(string apiPath, ApiToken? accessToken = null) where T : class?;

    //meta
    public Task<AbisConfigResponse?> GetAbisConfigAsync();
    public Task<List<ConfigResponse>?> GetConfigAsync(); // yeah i dont know.. it just sends in array..
    public Task<HealthResponse?> GetHealthAsync();

    //chain
    public Task<List<Killmail>> GetKillMailsAsync(int? limit = null);
    public Task<Killmail?> GetKillMailIdAsync(string id);
    public Task<List<SmartAssemblyReference>> GetSmartAssembliesAsync(SmartAssemblyType? type = null, int? limit = null);
    public Task<SmartAssemblyBase?> GetSmartAssemblyIdAsync(string id);
    public Task<List<SmartCharacterReference>> GetSmartCharactersAsync(int? limit = null);
    public Task<SmartCharacter?> GetSmartCharacterAdressAsync(string adress);

    //game
    public Task<List<FuelType>> GetFuelsAsync(int? limit = null);
    public Task<List<SmartCharacterJump>> GetSmartCharacterJumpsAsync(ApiToken accessToken, int? limit = null);
    public Task<SmartCharacterJump?> GetSmartCharacterJumpIdAsync(string id, ApiToken accessToken);
    public Task<List<SolarSystemReference>> GetSolarSystemsAsync(int? limit = null);
    public Task<SolarSystem?> GetSolarSystemIdAsync(string id);
    public Task<List<TribeReference>> GetTribesAsync(int? limit = null);
    public Task<Tribe?> GetTribeIdAsync(string id);
    public Task<List<TypeDetails>> GetTypesAsync(int? limit = null);
    public Task<TypeDetails?> GetTypeIdAsync(string id);

}
