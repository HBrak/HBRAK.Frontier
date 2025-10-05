using HBRAK.Frontier.Authorization.Data;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace HBRAK.Frontier.Authorization.Service;

public class AuthorizationService : IAuthorizationService
{
    private readonly ITokenStore _store;
    private readonly HttpClient _http = new HttpClient();
    private readonly ILogger<AuthorizationService> _logger;
    private readonly IOptions<AuthorizationServiceOptions> _options;
    public List<AccessToken> Tokens { get; } = [];

    public AuthorizationService(ILogger<AuthorizationService> logger, IOptions<AuthorizationServiceOptions> options, ITokenStore tokenStore)
    {
        _store = tokenStore;
        _logger = logger;
        _options = options;
    }

    public async Task<AccessToken?> AddTokenFromWebsiteCookie(string token, CancellationToken ct = default)
    {
        _logger.LogInformation("Adding token from website cookie");
        var result = AccessToken.FromWebsiteCookie(token)!;
        Tokens.Add(result);
        await _store.SaveAsync(result, ct);
        return result;
    }

    public async Task<AccessToken?> AuthorizeAsync(string redirectUri, IEnumerable<string> scopes, CancellationToken ct = default)
    {
        
        _logger.LogCritical("Authorization via browser is not implemented yet by CCP.");
        return await Task.FromResult<AccessToken?>(null); //this is not implemented yet on CCP side :(
    }

    public Task<IReadOnlyList<AccessToken>> LoadAllAsync(CancellationToken ct = default)
        => _store.LoadAllAsync(_options.Value.AppId, ct);

    public async Task LoadAndRefreshAllAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("Loading and refreshing all tokens");
        var savedTokens = await LoadAllAsync(ct);

        Tokens.Clear();

        Tokens.AddRange(await Task.WhenAll(
            savedTokens.Select(async t =>
            {
                var refreshed = await RefreshIfNeededAsync(t, ct);
                await _store.SaveAsync(refreshed, ct);
                return refreshed;
            })));
    }

    public async Task<AccessToken> RefreshIfNeededAsync(AccessToken token, CancellationToken ct = default)
    {
        if (token is null) throw new ArgumentNullException(nameof(token));
        if (string.IsNullOrEmpty(token.RefreshToken))
            return token; // no offline access → nothing to refresh

        _logger.LogInformation($"{token.EveSub} token expires at {token.ExpiresAt}, it is now {DateTimeOffset.UtcNow}");
        if (DateTimeOffset.UtcNow < token.ExpiresAt - TimeSpan.FromMinutes(1))
            return token; // still valid

        return await RefreshAsync(token, ct);

        
    }

    public async Task<AccessToken> RefreshAsync(AccessToken token, CancellationToken ct)
    {
        _logger.LogInformation($"Refreshing token for {token.EveSub} ");
        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "refresh_token",
            ["refresh_token"] = token.RefreshToken,
            ["client_id"] = token.ApplicationId
        });

        using var req = new HttpRequestMessage(HttpMethod.Post, _options.Value.AuthUrl)
        {
            Content = content
        };
        using var resp = await _http.SendAsync(req, ct);

        if (!resp.IsSuccessStatusCode)
        {
            _logger.LogError($"Failed to refresh token for {token.EveSub}, deleting token");
            await _store.DeleteAsync(_options.Value.AppId, token.Sub!, ct);
            throw new UnauthorizedAccessException(
                $"Refresh failed: {(int)resp.StatusCode} {resp.ReasonPhrase}");
        }

        var json = await resp.Content.ReadAsStringAsync(ct);

        AccessToken result = AccessToken.FromRefreshResponse(json)!;

        await _store.SaveAsync(result, ct);
        return result;
    }
}
