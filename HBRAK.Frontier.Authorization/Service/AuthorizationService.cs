using HBRAK.Frontier.Authorization.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace HBRAK.Frontier.Authorization.Service;

public class AuthorizationService : IAuthorizationService
{
    private readonly ITokenStore _store;
    private readonly HttpClient _http;
    public List<AccessToken> Tokens { get; } = [];
    private string _appId;

    public AuthorizationService(string appId, ITokenStore? store = null, HttpClient? http = null)
    {
        _store = store ?? new WindowsDpapiTokenStore();
        _http = http ?? new HttpClient();
        _appId = appId;

        _http.DefaultRequestHeaders.UserAgent.ParseAdd("HBRAK.Frontier.Auth/1.0");
    }

    public async Task<AccessToken> AddTokenFromWebsiteCookie(string token, CancellationToken ct = default)
    {
        var result = AccessToken.FromWebsiteCookie(token);
        Tokens.Add(result);
        await _store.SaveAsync(result, ct);
        return result;
    }

    public async Task<AccessToken?> AuthorizeAsync(string redirectUri, IEnumerable<string> scopes, CancellationToken ct = default)
    {
        return null; //this is not implemented yet on CCP side :(
    }

    public Task<IReadOnlyList<AccessToken>> LoadAllAsync(CancellationToken ct = default)
        => _store.LoadAllAsync(_appId, ct);

    public async Task LoadAndRefreshAllAsync(CancellationToken ct = default)
    {
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

        if (DateTimeOffset.UtcNow < token.ExpiresAt - TimeSpan.FromMinutes(1))
            return token; // still valid

        return await RefreshAsync(token, ct);

        
    }

    public async Task<AccessToken> RefreshAsync(AccessToken token, CancellationToken ct)
    {
        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "refresh_token",
            ["refresh_token"] = token.RefreshToken,
            ["client_id"] = token.ApplicationId
        });

        using var req = new HttpRequestMessage(HttpMethod.Post, "https://auth.evefrontier.com/oauth2/token")
        {
            Content = content
        };
        using var resp = await _http.SendAsync(req, ct);

        if (!resp.IsSuccessStatusCode)
        {
            await _store.DeleteAsync(_appId, token.Sub!, ct);
            throw new UnauthorizedAccessException(
                $"Refresh failed: {(int)resp.StatusCode} {resp.ReasonPhrase}");
        }

        var json = await resp.Content.ReadAsStringAsync(ct);
        AccessToken result = AccessToken.FromRefreshResponse(json);

        await _store.SaveAsync(result, ct);
        return result;
    }
}
