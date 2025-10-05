using System;
using System.Buffers.Text;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace HBRAK.Frontier.Authorization.Data;

public class AccessToken
{
    public string Token { get; protected set; } = string.Empty;
    [JsonPropertyName("aud")] public string? Aud { get; set; }
    [JsonPropertyName("exp")] public long? Exp { get; set; }
    [JsonPropertyName("iat")] public long? Iat { get; set; }
    [JsonPropertyName("iss")] public string? Iss { get; set; }
    [JsonPropertyName("sub")] public required string Sub { get; set; }
    [JsonPropertyName("jti")] public string? Jti { get; set; }
    [JsonPropertyName("authenticationType")] public string? AuthenticationType { get; set; }
    [JsonPropertyName("applicationId")] public required string ApplicationId { get; set; }
    [JsonPropertyName("scope")] public string? Scope { get; set; }
    [JsonPropertyName("roles")] public string[]? Roles { get; set; }
    [JsonPropertyName("sid")] public string? Sid { get; set; }
    [JsonPropertyName("auth_time")] public long? AuthTime { get; set; }
    [JsonPropertyName("tid")] public string? Tid { get; set; }
    [JsonPropertyName("scp")] public string? Scp { get; set; }
    [JsonPropertyName("ccp_owned_wallet_address")] public string? CcpOwnedWalletAddress { get; set; }
    [JsonPropertyName("tier")] public string? Tier { get; set; }
    [JsonPropertyName("azp")] public string? Azp { get; set; }
    [JsonPropertyName("eve_sub")] public string? EveSub { get; set; }
    [JsonPropertyName("tenant")] public string? Tenant { get; set; }
    [JsonPropertyName("email")] public string? Email { get; set; }
    [JsonPropertyName("refreshToken")] public string RefreshToken { get; set; } = string.Empty;

    [JsonIgnore] public DateTimeOffset? ExpiresAt => Exp.HasValue ? DateTimeOffset.FromUnixTimeSeconds(Exp.Value) : null;

    public string[] GetScopes()
    {
        var s = !string.IsNullOrWhiteSpace(Scope) ? Scope : Scp;
        return string.IsNullOrWhiteSpace(s)
            ? Array.Empty<string>()
            : s.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }

    public static AccessToken? FromToken(string? token)
    {
        if (string.IsNullOrWhiteSpace(token)) return null;

        var payloadJson = GetPayloadJson(token);
        var claims = JsonSerializer.Deserialize<AccessToken>(payloadJson)!;
        claims.Token = token;
        return claims;
    }

    public static AccessToken? FromWebsiteCookie(string jwt)
    {
        string payloadJson = GetPayloadJson(jwt);
        string? accessCode = JsonDocument.Parse(payloadJson)
            .RootElement
            .GetProperty("accessToken")
            .GetString();

        AccessToken? token = FromToken(accessCode) ?? null;

        if (token != null)
        {
            token.RefreshToken = JsonDocument.Parse(payloadJson)
                .RootElement
                .GetProperty("refreshToken")
                .GetString()!;
        }
        return token;
    }

    public static AccessToken? FromRefreshResponse(string response)
    {
        string? accessCode = JsonDocument.Parse(response)
            .RootElement
            .GetProperty("access_token")
            .GetString();

        AccessToken? token = FromToken(accessCode) ?? null;

        if (token != null)
        {
            token.RefreshToken = JsonDocument.Parse(response)
                .RootElement
                .GetProperty("refresh_token")
                .GetString()!;
        }
        return token;
    }

    public static string Encode(byte[] bytes)
    => Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    public static byte[] Decode(string s)
    {
        s = s.Replace('-', '+').Replace('_', '/');
        switch (s.Length % 4)
        {
            case 2: s += "=="; break;
            case 3: s += "="; break;
        }
        return Convert.FromBase64String(s);
    }
    public static string GetPayloadJson(string jwt)
    {
        var parts = jwt.Split('.');
        if (parts.Length < 2) throw new FormatException("Invalid JWT");
        return System.Text.Encoding.UTF8.GetString(Decode(parts[1]));
    }
}
