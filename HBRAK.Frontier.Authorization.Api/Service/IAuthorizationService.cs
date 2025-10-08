using HBRAK.Frontier.Authorization.Api.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HBRAK.Frontier.Authorization.Api.Service;

public interface IAuthorizationService
{
    public List<ApiToken> Tokens { get; }
    public Task<ApiToken?> AddTokenFromWebsiteCookie(string token, CancellationToken ct = default);
    public Task LoadAndRefreshAllAsync(CancellationToken ct = default);
    public Task<IReadOnlyList<ApiToken>> LoadAllAsync(CancellationToken ct = default);
    public Task<ApiToken?> AuthorizeAsync(
        string redirectUri,
        IEnumerable<string> scopes,
        CancellationToken ct = default);
    public Task<ApiToken> RefreshIfNeededAsync(ApiToken token, CancellationToken ct = default);

    public Task<ApiToken> RefreshAsync(ApiToken token, CancellationToken ct = default);

}
