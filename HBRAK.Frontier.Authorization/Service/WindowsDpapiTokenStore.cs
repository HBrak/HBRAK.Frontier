using HBRAK.Frontier.Authorization.Data;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Runtime.Versioning;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace HBRAK.Frontier.Authorization.Service;

[SupportedOSPlatform("windows")]
public sealed class WindowsDpapiTokenStore : ITokenStore
{
    private readonly string _root;
    private readonly byte[] _entropy;
    private static readonly JsonSerializerOptions _json = CreateJsonOptions();

    public WindowsDpapiTokenStore(ILogger<WindowsDpapiTokenStore> logger, IOptions<AuthorizationServiceOptions> options)
    {
        _root = Environment.ExpandEnvironmentVariables(options.Value.TokenStoragePath);
        Directory.CreateDirectory(_root);
        _entropy = System.Text.Encoding.UTF8.GetBytes("HBRAK.Frontier.Authorization.TokenEntropy:v1");
    }

    public Task SaveAsync(AccessToken token, CancellationToken ct = default)
    {
        if (token is null) throw new ArgumentNullException(nameof(token));
        if (string.IsNullOrWhiteSpace(token.ApplicationId))
            throw new InvalidOperationException("AccessToken.ApplicationId is null/empty; cannot determine application folder.");
        if (string.IsNullOrWhiteSpace(token.Sub))
            throw new InvalidOperationException("AccessToken.Sub is null/empty; cannot determine filename.");

        var appId = token.ApplicationId!;
        var sub = token.Sub!;

        Directory.CreateDirectory(AppFolder(appId));
        var path = TokenPath(appId, sub);

        var json = JsonSerializer.Serialize(token, _json);
        var plain = System.Text.Encoding.UTF8.GetBytes(json);
        var cipher = ProtectedData.Protect(plain, _entropy, DataProtectionScope.CurrentUser);

        return File.WriteAllBytesAsync(path, cipher, ct);
    }

    public async Task<AccessToken?> LoadAsync(string appId, string sub, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(appId) || string.IsNullOrWhiteSpace(sub)) return null;

        var path = TokenPath(appId, sub);
        if (!File.Exists(path)) return null;

        var cipher = await File.ReadAllBytesAsync(path, ct);
        var plain = ProtectedData.Unprotect(cipher, _entropy, DataProtectionScope.CurrentUser);

        return JsonSerializer.Deserialize<AccessToken>(plain, _json);
    }

    // Loads all tokens for a given application (client) id
    public async Task<IReadOnlyList<AccessToken>> LoadAllAsync(string appId, CancellationToken ct = default)
    {
        var folder = AppFolder(appId);
        if (!Directory.Exists(folder)) return Array.Empty<AccessToken>();

        var list = new List<AccessToken>();
        foreach (var file in Directory.EnumerateFiles(folder, "*.bin", SearchOption.TopDirectoryOnly))
        {
            try
            {
                var cipher = await File.ReadAllBytesAsync(file, ct);
                var plain = ProtectedData.Unprotect(cipher, _entropy, DataProtectionScope.CurrentUser);
                var tok = JsonSerializer.Deserialize<AccessToken>(plain, _json);
                if (tok != null) list.Add(tok);
            }
            catch
            {
                // Skip corrupted/unreadable entries, but don't crash the whole load
            }
        }
        return list;
    }

    public Task DeleteAsync(string appId, string sub, CancellationToken ct = default)
    {
        var path = TokenPath(appId, sub);
        if (File.Exists(path)) File.Delete(path);
        return Task.CompletedTask;
    }

    private string AppFolder(string appId) => Path.Combine(_root, Sanitize(appId));
    private string TokenPath(string appId, string sub) => Path.Combine(AppFolder(appId), $"{Sanitize(sub)}.bin");

    private static string Sanitize(string name)
    {
        foreach (var c in Path.GetInvalidFileNameChars()) name = name.Replace(c, '_');
        return name;
    }

    private static JsonSerializerOptions CreateJsonOptions()
    {
        var opts = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };
        return opts;
    }
}
