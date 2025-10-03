using HBRAK.Frontier.Api.Data;
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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace HBRAK.Frontier.Api.Service;

public class ApiService : IApiService
{
    private HttpClient _http;

    public ApiService()
    {
        _http = new HttpClient
        {
            BaseAddress = new Uri("https://world-api-stillness.live.tech.evefrontier.com")
        };
    }

    private void SetAuthorizationHeader(AccessToken? accessToken)
    {
        if (accessToken != null)
        {
            _http.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken.Token);
        }
        else
        {
            _http.DefaultRequestHeaders.Authorization = null;
        }
    }

    public async Task<T?> GetFromApiAsync<T>(string apiPath, AccessToken? accessToken = null) where T : class
    {
        SetAuthorizationHeader(accessToken);

        var response = await _http.GetAsync(apiPath);

        if (!response.IsSuccessStatusCode)
        {
            Console.WriteLine($"Error fetching {apiPath}: {response.StatusCode}");
            return null;
        }

        var json = await response.Content.ReadAsStringAsync();

        if (typeof(T) == typeof(string))
        {
            return (T)(object)json;
        }
        return JsonSerializer.Deserialize<T>(json);
    }

    public async Task<List<T>> GetListFromApiAsync<T>(string apiPath, AccessToken? accessToken = null, int limit = 10, Dictionary<string, string>? extraParams = null) where T : class
    {
        int offset = 0;
        List<T> listItems = [];

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

            List<T> resItems = JsonSerializer.Deserialize<List<T>>(res.Data) ?? [];
            
            listItems.AddRange(resItems);


            if (res.MetaData.Total <= offset + limit)
            {
                break;
            }

            offset += resItems.Count;
        }

        return listItems;

    }

    public async Task<ConfigAbiResponse?> GetAbisConfigAsync()
    {
        return await GetFromApiAsync<ConfigAbiResponse?>("abis/config");
    }

    public async Task<List<ConfigResponse>?> GetConfigAsync()
    {
        return await GetFromApiAsync<List<ConfigResponse>?>("config");
    }

    public async Task<HealthResponse?> GetHealthAsync()
    {
        return await GetFromApiAsync<HealthResponse?>("health");
    }

    public async Task<List<Killmail>> GetKillMailsAsync(int limit = 100)
    {
        return await GetListFromApiAsync<Killmail>("v2/killmails", null, limit);
    }

    public async Task<Killmail?> GetKillMailIdAsync(string id)
    {
        return await GetFromApiAsync<Killmail?>($"v2/killmails/{id}");
    }

    public async Task<List<SmartAssemblyReference>> GetSmartAssembliesAsync(SmartAssemblyType? type = null, int limit = 100)
    {
        Dictionary<string, string>? param = null;
        if (type != null)
        {
            param = new Dictionary<string, string>
            {
                { "type", type.Value.ToString() }
            };
        }
        return await GetListFromApiAsync<SmartAssemblyReference>("v2/smartassemblies", null, limit, param);
    }

    public async Task<SmartAssemblyBase?> GetSmartAssemblyIdAsync(string id)
    {
        return await GetFromApiAsync<SmartAssemblyBase?>($"v2/smartassemblies/{id}");
    }

    public async Task<List<SmartCharacterReference>> GetSmartCharactersAsync(int limit = 100)
    {
        return await GetListFromApiAsync<SmartCharacterReference>("v2/smartcharacters", null, limit);
    }

    public async Task<SmartCharacter?> GetSmartCharacterAdressAsync(string adress)
    {
        return await GetFromApiAsync<SmartCharacter?>($"v2/smartcharacters/{adress}");
    }

    public async Task<List<FuelType>> GetFuelsAsync(int limit = 100)
    {
        return await GetListFromApiAsync<FuelType>("v2/fuels", null, limit);
    }

    public async Task<List<SmartCharacterJump>> GetSmartCharacterJumpsAsync(AccessToken accessToken, int limit = 100)
    {
        return await GetListFromApiAsync<SmartCharacterJump>("v2/smartcharacters/me/jumps", accessToken, limit);
    }

    public async Task<SmartCharacterJump?> GetSmartCharacterJumpIdAsync(string id, AccessToken accessToken)
    {
        return await GetFromApiAsync<SmartCharacterJump?>($"v2/smartcharacters/me/jumps/{id}", accessToken);
    }

    public async Task<List<SolarSystemReference>> GetSolarSystemsAsync(int limit = 100)
    {
        return await GetListFromApiAsync<SolarSystemReference>("v2/solarsystems", null, limit);
    }

    public async Task<SolarSystem?> GetSolarSystemIdAsync(string id)
    {
        return await GetFromApiAsync<SolarSystem?>($"v2/solarsystems/{id}");
    }

    public async Task<List<TribeReference>> GetTribesAsync(int limit = 100)
    {
        return await GetListFromApiAsync<TribeReference>("v2/tribes", null, limit);
    }

    public async Task<Tribe?> GetTribeIdAsync(string id)
    {
        return await GetFromApiAsync<Tribe?>($"v2/tribes/{id}");
    }

    public async Task<List<TypeDetails>> GetTypesAsync(int limit = 100)
    {
        return await GetListFromApiAsync<TypeDetails>("v2/types", null, limit);
    }

    public async Task<TypeDetails?> GetTypeIdAsync(string id)
    {
        return await GetFromApiAsync<TypeDetails?>($"v2/types/{id}");
    }
}
