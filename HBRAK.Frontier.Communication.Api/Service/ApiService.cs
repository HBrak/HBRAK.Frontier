using HBRAK.Frontier.Authorization.Api.Data;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using System.Runtime.InteropServices.Marshalling;
using Microsoft.Extensions.Options;
using HBRAK.Frontier.Communication.Api.Data.Chain.SmartCharacters;
using HBRAK.Frontier.Communication.Api.Data;
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

public class ApiService : IApiService
{
    private HttpClient _http;
    private readonly ILogger<ApiService> _logger;
    private readonly IOptions<ApiServiceOptions> _options;

    public ApiService(ILogger<ApiService> logger, IOptions<ApiServiceOptions> options)
    {
        _logger = logger;
        _options = options;

        _http = new HttpClient
        {
            BaseAddress = new Uri(_options.Value.BaseUrl),
            Timeout = TimeSpan.FromSeconds(_options.Value.TimeoutSeconds)
        };
    }

    private void SetAuthorizationHeader(ApiToken? accessToken)
    {
        if (accessToken != null)
        {
            _logger.LogInformation("Using access token for user {Sub}", accessToken.Sub);
            _http.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken.Token);
        }
        else
        {
            _http.DefaultRequestHeaders.Authorization = null;
        }
    }

    public async Task<T?> GetFromApiAsync<T>(string apiPath, ApiToken? accessToken = null) where T : class?
    {
        SetAuthorizationHeader(accessToken);

        var response = await _http.GetAsync(apiPath);

        if (!response.IsSuccessStatusCode)
        { 
            _logger.LogWarning($"Error fetching {apiPath}: {response.StatusCode}");
            return null;
        }
        
        var json = await response.Content.ReadAsStringAsync();
        _logger.LogDebug($"{apiPath} : {json}");

        if (typeof(T) == typeof(string))
        {
            return (T)(object)json;
        }
        return JsonSerializer.Deserialize<T>(json);
    }

    public async Task<List<T>> GetListFromApiAsync<T>(string apiPath, ApiToken? accessToken = null, int? limit = null, Dictionary<string, string>? extraParams = null) where T : class?
    {
        int offset = 0;
        List<T> listItems = [];

        if (_options.Value.DefaultLimit <= 0 || _options.Value.DefaultLimit > _options.Value.MaxLimit)
        {
            _options.Value.DefaultLimit = Math.Min(_options.Value.DefaultLimit, _options.Value.MaxLimit);
        }

        if (limit == null)
        {
            limit = _options.Value.DefaultLimit;
        }else if (limit.Value > _options.Value.MaxLimit)
        {
            limit = _options.Value.MaxLimit;
        }

        string extraQuery = string.Empty;
        if (extraParams is not null && extraParams.Count > 0)
        {
            var encoded = extraParams
                .Select(kv => $"{Uri.EscapeDataString(kv.Key)}={Uri.EscapeDataString(kv.Value)}");
            extraQuery = "&" + string.Join("&", encoded);
        }

        while (true)
        {
            var res = await GetFromApiAsync<ListResponse?>($"{apiPath}?limit={limit}&offset={offset}{extraQuery}", accessToken);

            if (res == null)
            {
                break;
            }

            List<T> resItems = res.Data.Deserialize<List<T>>() ?? [];
            
            listItems.AddRange(resItems);


            if (res.MetaData.Total <= offset + limit)
            {
                break;
            }

            offset += resItems.Count;
        }

        return listItems;

    }

    public async Task<AbisConfigResponse?> GetAbisConfigAsync()
    {
        return await GetFromApiAsync<AbisConfigResponse?>(_options.Value.EndpointAbisConfig);
    }

    public async Task<List<ConfigResponse>?> GetConfigAsync()
    {
        return await GetFromApiAsync<List<ConfigResponse>?>(_options.Value.EndpointConfig);
    }

    public async Task<HealthResponse?> GetHealthAsync()
    {
        return await GetFromApiAsync<HealthResponse?>(_options.Value.EndpointHealth);
    }

    public async Task<List<Killmail>> GetKillMailsAsync(int? limit = null)
    {
        return await GetListFromApiAsync<Killmail>(_options.Value.EndpointKillMails, null, limit);
    }

    public async Task<Killmail?> GetKillMailIdAsync(string id)
    {
        return await GetFromApiAsync<Killmail?>($"{_options.Value.EndpointKillMails}/{id}");
    }

    public async Task<List<SmartAssemblyReference>> GetSmartAssembliesAsync(SmartAssemblyType? type = null, int? limit = null)
    {
        Dictionary<string, string>? param = null;
        if (type != null)
        {
            param = new Dictionary<string, string>
            {
                { "type", type.Value.ToString() }
            };
        }
        return await GetListFromApiAsync<SmartAssemblyReference>(_options.Value.EndpointSmartAssemblies, null, limit, param);
    }

    public async Task<SmartAssemblyBase?> GetSmartAssemblyIdAsync(string id)
    {
        return await GetFromApiAsync<SmartAssemblyBase?>($"{_options.Value.EndpointSmartAssemblies}/{id}");
    }

    public async Task<List<SmartCharacterReference>> GetSmartCharactersAsync(int? limit = null)
    {
        return await GetListFromApiAsync<SmartCharacterReference>(_options.Value.EndpointSmartCharacters, null, limit);
    }

    public async Task<SmartCharacter?> GetSmartCharacterAdressAsync(string adress)
    {
        return await GetFromApiAsync<SmartCharacter?>($"{_options.Value.EndpointSmartCharacters}/{adress}");
    }

    public async Task<List<FuelType>> GetFuelsAsync(int? limit = null)
    {
        return await GetListFromApiAsync<FuelType>(_options.Value.EndpointFuels, null, limit);
    }

    public async Task<List<SmartCharacterJump>> GetSmartCharacterJumpsAsync(ApiToken accessToken, int? limit = null)
    {
        return await GetListFromApiAsync<SmartCharacterJump>(_options.Value.EndpointSmartCharacterJumps, accessToken, limit);
    }

    public async Task<SmartCharacterJump?> GetSmartCharacterJumpIdAsync(string id, ApiToken accessToken)
    {
        return await GetFromApiAsync<SmartCharacterJump?>($"{_options.Value.EndpointSmartCharacterJumps}/{id}", accessToken);
    }

    public async Task<List<SolarSystemReference>> GetSolarSystemsAsync(int? limit = null)
    {
        return await GetListFromApiAsync<SolarSystemReference>(_options.Value.EndpointSolarSystems, null, limit);
    }

    public async Task<SolarSystem?> GetSolarSystemIdAsync(string id)
    {
        return await GetFromApiAsync<SolarSystem?>($"{_options.Value.EndpointSolarSystems}/{id}");
    }

    public async Task<List<TribeReference>> GetTribesAsync(int? limit = null)
    {
        return await GetListFromApiAsync<TribeReference>(_options.Value.EndpointTribes, null, limit);
    }

    public async Task<Tribe?> GetTribeIdAsync(string id)
    {
        return await GetFromApiAsync<Tribe?>($"{_options.Value.EndpointTribes}/{id}");
    }

    public async Task<List<TypeDetails>> GetTypesAsync(int? limit = null)
    {
        return await GetListFromApiAsync<TypeDetails>(_options.Value.EndpointTypes, null, limit);
    }

    public async Task<TypeDetails?> GetTypeIdAsync(string id)
    {
        return await GetFromApiAsync<TypeDetails?>($"{_options.Value.EndpointTypes}/{id}");
    }
}
