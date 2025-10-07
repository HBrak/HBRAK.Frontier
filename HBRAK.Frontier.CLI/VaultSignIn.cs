using Nethereum.Signer;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace HBRAK.Frontier.CLI;

public class VaultSignIn
{
    public sealed record Result(string Address, string Signature, string Nonce, long ChainId);

    public async Task<Result?> RunAsync(long expectedChainId = 695569, int? port = null, CancellationToken ct = default)
    {
        // Choose a port & start listener
        var p = port ?? Random.Shared.Next(49152, 65535);
        var url = $"http://127.0.0.1:{p}/";
        using var listener = new HttpListener();
        listener.Prefixes.Add(url);
        listener.Start();

        // Open browser (simple)
        TryOpenBrowser(url);

        // One-time nonce
        var nonce = Guid.NewGuid().ToString("N");

        // Paths (same directory as the exe)
        var root = AppContext.BaseDirectory;
        var htmlPath = Path.Combine(root, "login.html");
        var cssPath = Path.Combine(root, "login.css");

        Result? result = null;

        while (result is null)
        {
            var ctx = await listener.GetContextAsync();

            var path = ctx.Request.Url?.AbsolutePath ?? "/";
            if (path == "/")
            {
                // Serve HTML with placeholder replacement
                if (!File.Exists(htmlPath))
                {
                    await RespondText(ctx.Response, 500, "login.html not found next to the executable.");
                    continue;
                }

                var html = await File.ReadAllTextAsync(htmlPath, ct);
                html = html.Replace("__CHAIN_ID__", expectedChainId.ToString())
                           .Replace("__NONCE__", nonce);

                var bytes = Encoding.UTF8.GetBytes(html);
                ctx.Response.ContentType = "text/html; charset=utf-8";
                ctx.Response.ContentLength64 = bytes.Length;
                await ctx.Response.OutputStream.WriteAsync(bytes, 0, bytes.Length, ct);
                ctx.Response.Close();
            }
            else if (path == "/login.css")
            {
                // Serve CSS as-is
                if (!File.Exists(cssPath))
                {
                    await RespondText(ctx.Response, 500, "login.css not found next to the executable.");
                    continue;
                }

                var css = await File.ReadAllTextAsync(cssPath, ct);
                var bytes = Encoding.UTF8.GetBytes(css);
                ctx.Response.ContentType = "text/css; charset=utf-8";
                ctx.Response.ContentLength64 = bytes.Length;
                await ctx.Response.OutputStream.WriteAsync(bytes, 0, bytes.Length, ct);
                ctx.Response.Close();
            }
            else if (path == "/callback" && ctx.Request.HttpMethod == "POST")
            {
                // Receive signed payload
                using var sr = new StreamReader(ctx.Request.InputStream, Encoding.UTF8);
                var body = await sr.ReadToEndAsync(ct);

                var payload = JsonSerializer.Deserialize<WalletCallback>(
                    body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (payload is not null &&
                    !string.IsNullOrWhiteSpace(payload.Address) &&
                    !string.IsNullOrWhiteSpace(payload.Signature) &&
                    !string.IsNullOrWhiteSpace(payload.Message) &&
                    payload.ChainId == expectedChainId &&
                    string.Equals(payload.Nonce, nonce, StringComparison.Ordinal))
                {
                    // Verify EXACT message (as signed in the page)
                    var signer = new EthereumMessageSigner();
                    var recovered = signer.EncodeUTF8AndEcRecover(payload.Message, payload.Signature);

                    if (recovered.Equals(payload.Address, StringComparison.OrdinalIgnoreCase))
                    {
                        result = new Result(payload.Address, payload.Signature, nonce, payload.ChainId);
                        await RespondText(ctx.Response, 200, "OK. You may close this tab.");
                        break;
                    }
                }

                await RespondText(ctx.Response, 400, "Invalid signature or payload.");
            }
            else
            {
                await RespondText(ctx.Response, 404, "Not found.");
            }
        }

        listener.Stop();
        return result;
    }

    private static Task RespondText(HttpListenerResponse res, int code, string msg)
    {
        res.StatusCode = code;
        var bytes = Encoding.UTF8.GetBytes(msg);
        res.ContentType = "text/plain; charset=utf-8";
        res.ContentLength64 = bytes.Length;
        return res.OutputStream.WriteAsync(bytes, 0, bytes.Length);
    }

    private static bool TryOpenBrowser(string url)
    {
        try
        {
            Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true });
            return true;
        }
        catch { return false; }
    }

    private sealed class WalletCallback
    {
        public string Address { get; set; } = "";
        public string Signature { get; set; } = "";
        public long ChainId { get; set; }
        public string Nonce { get; set; } = "";
        public string Message { get; set; } = "";
    }
}
