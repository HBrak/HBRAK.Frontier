using HBRAK.Frontier.Authorization.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HBRAK.Frontier.Authorization.Service;

public interface IAuthorizationService
{
    public List<AccessToken> Tokens { get; }
    public Task<AccessToken?> AddTokenFromWebsiteCookie(string token, CancellationToken ct = default);
    public Task LoadAndRefreshAllAsync(CancellationToken ct = default);
    public Task<IReadOnlyList<AccessToken>> LoadAllAsync(CancellationToken ct = default);
    public Task<AccessToken?> AuthorizeAsync(
        string redirectUri,
        IEnumerable<string> scopes,
        CancellationToken ct = default);
    public Task<AccessToken> RefreshIfNeededAsync(AccessToken token, CancellationToken ct = default);

    public Task<AccessToken> RefreshAsync(AccessToken token, CancellationToken ct = default);

}
