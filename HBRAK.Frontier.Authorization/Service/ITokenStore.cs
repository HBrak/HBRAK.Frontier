using HBRAK.Frontier.Authorization.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HBRAK.Frontier.Authorization.Service;

public interface ITokenStore
{
    Task SaveAsync(AccessToken token, CancellationToken ct = default);
    Task<AccessToken?> LoadAsync(string appId, string sub, CancellationToken ct = default);
    Task<IReadOnlyList<AccessToken>> LoadAllAsync(string appId, CancellationToken ct = default);
    Task DeleteAsync(string appId, string sub, CancellationToken ct = default);
}
