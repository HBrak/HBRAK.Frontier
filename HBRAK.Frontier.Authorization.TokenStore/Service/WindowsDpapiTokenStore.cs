using HBRAK.Frontier.Authorization.TokenStore.Service;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Runtime.Versioning;
using System.Security.Cryptography;
using System.Text.Json;

namespace HBRAK.Frontier.Authorization.Service;

[SupportedOSPlatform("windows")]
public sealed class WindowsDpapiTokenStore : ITokenStore
{
    private readonly string _root;
    private readonly byte[] _entropy;
    private static readonly JsonSerializerOptions _json = CreateJsonOptions();

    public WindowsDpapiTokenStore(ILogger<WindowsDpapiTokenStore> logger, IOptions<WindowsDpapiTokenStoreOptions> options)
    {
        _root = Environment.ExpandEnvironmentVariables(options.Value.TokenStoragePath);
        Directory.CreateDirectory(_root);
        _entropy = System.Text.Encoding.UTF8.GetBytes("HBRAK.Frontier.Authorization.TokenEntropy:v1");
    }

    public Task SaveAsync<T>(T token, string applicationId, string account, CancellationToken ct = default) where T : class
    {
        if (token is null) throw new ArgumentNullException(nameof(token));
        if (string.IsNullOrWhiteSpace(applicationId))
            throw new InvalidOperationException("applicationId is null/empty; cannot determine application folder.");
        if (string.IsNullOrWhiteSpace(account))
            throw new InvalidOperationException("account is null/empty; cannot determine filename.");

        Directory.CreateDirectory(AccountFolder(applicationId, account));
        var path = TokenPath(token,applicationId, account);

        var json = JsonSerializer.Serialize(token, _json);
        var plain = System.Text.Encoding.UTF8.GetBytes(json);
        var cipher = ProtectedData.Protect(plain, _entropy, DataProtectionScope.CurrentUser);

        return File.WriteAllBytesAsync(path, cipher, ct);
    }

    public async Task<T?> LoadAsync<T>(string appId, string account, CancellationToken ct = default) where T : class
    {

        if (string.IsNullOrWhiteSpace(appId) || string.IsNullOrWhiteSpace(account)) return null;
        var ty = typeof(T);


        var path = TokenPath(ty, appId, account);
        if (!File.Exists(path)) return null;

        var cipher = await File.ReadAllBytesAsync(path, ct);
        var plain = ProtectedData.Unprotect(cipher, _entropy, DataProtectionScope.CurrentUser);

        return JsonSerializer.Deserialize<T>(plain, _json);
    }

    public async Task<IReadOnlyList<T>> LoadAllAsync<T>(string appId, CancellationToken ct = default) where T : class
    {
        var folder = AppFolder(appId);
        if (!Directory.Exists(folder)) return Array.Empty<T>();

        var list = new List<T>();
        foreach (var file in Directory.EnumerateFiles(folder, "*.bin", SearchOption.AllDirectories))
        {
            try
            {
                var cipher = await File.ReadAllBytesAsync(file, ct);
                var plain = ProtectedData.Unprotect(cipher, _entropy, DataProtectionScope.CurrentUser);
                var tok = JsonSerializer.Deserialize<T>(plain, _json);
                if (tok != null) list.Add(tok);
            }
            catch
            {
                // Skip corrupted/unreadable entries, but don't crash the whole load
            }
        }
        return list;
    }

    public Task DeleteAsync(string appId, string account, CancellationToken ct = default)
    {
        var path = AccountFolder(appId, account);
        if (File.Exists(path)) File.Delete(path);
        return Task.CompletedTask;
    }

    private string AccountFolder(string appId, string account) => Path.Combine(AppFolder(appId), Sanitize(account));
    private string AppFolder(string appId) => Path.Combine(_root, Sanitize(appId));

    private string TokenPath<T>(T token,string appId, string account) => Path.Combine(AccountFolder(appId, account), $"{Sanitize(token!.GetType().Name)}.bin");

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
