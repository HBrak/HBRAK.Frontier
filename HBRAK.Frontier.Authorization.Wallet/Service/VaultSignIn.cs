using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Nethereum.Signer;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text.Json;

namespace HBRAK.Frontier.CLI;

public class VaultSignIn
{
    public sealed record Result(string Address, string Signature, string Nonce, long ChainId);

    public async Task<Result?> RunAsync(long expectedChainId = 695569, CancellationToken ct = default)
    {
        var nonce = Guid.NewGuid().ToString("N");
        var root = AppContext.BaseDirectory;
        var htmlPath = Path.Combine(root, "WebPage\\login.html");
        var cssPath = Path.Combine(root, "WebPage\\login.css");

        if (!File.Exists(htmlPath)) throw new FileNotFoundException("login.html not found", htmlPath);
        if (!File.Exists(cssPath)) throw new FileNotFoundException("login.css not found", cssPath);

        var htmlTemplate = await File.ReadAllTextAsync(htmlPath, ct);
        var cssContent = await File.ReadAllTextAsync(cssPath, ct);

        var tcs = new TaskCompletionSource<Result?>(TaskCreationOptions.RunContinuationsAsynchronously);

        // ✅ Pick a free local port first — no IServerAddressesFeature needed
        var port = GetFreeTcpPort();

        var app = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            ContentRootPath = root
        })
        .WithKestrelOnLoopback(port)
        .Build();

        // Routes
        app.MapGet("/", async ctx =>
        {
            var html = htmlTemplate
                .Replace("__CHAIN_ID__", expectedChainId.ToString())
                .Replace("__NONCE__", nonce);

            ctx.Response.ContentType = "text/html; charset=utf-8";
            await ctx.Response.WriteAsync(html);
        });

        app.MapGet("/login.css", async ctx =>
        {
            ctx.Response.ContentType = "text/css; charset=utf-8";
            await ctx.Response.WriteAsync(cssContent);
        });

        app.MapPost("/callback", async ctx =>
        {
            var payload = await JsonSerializer.DeserializeAsync<WalletCallback>(
                ctx.Request.Body,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true },
                ct);

            if (payload is not null &&
                !string.IsNullOrWhiteSpace(payload.Address) &&
                !string.IsNullOrWhiteSpace(payload.Signature) &&
                !string.IsNullOrWhiteSpace(payload.Message) &&
                payload.ChainId == expectedChainId &&
                string.Equals(payload.Nonce, nonce, StringComparison.Ordinal))
            {
                try
                {
                    var signer = new EthereumMessageSigner();
                    var recovered = signer.EncodeUTF8AndEcRecover(payload.Message, payload.Signature);

                    if (recovered.Equals(payload.Address, StringComparison.OrdinalIgnoreCase))
                    {
                        await ctx.Response.WriteAsync("OK. You may close this tab.");
                        tcs.TrySetResult(new Result(payload.Address, payload.Signature, nonce, payload.ChainId));
                        return;
                    }
                }
                catch { /* fall through */ }
            }

            ctx.Response.StatusCode = 400;
            await ctx.Response.WriteAsync("Invalid signature or payload.");
        });

        await app.StartAsync(ct);

        // Open pretty local URL (host header doesn't matter)
        var prettyUrl = $"http://hbrak-frontier.localhost:{port}/";
        TryOpenBrowser(prettyUrl);

        using var _ = ct.Register(() => tcs.TrySetCanceled(ct));
        Result? result = null;
        try { result = await tcs.Task; }
        finally { await app.StopAsync(); await app.DisposeAsync(); }

        return result;
    }

    private static int GetFreeTcpPort()
    {
        using var l = new TcpListener(IPAddress.Loopback, 0);
        l.Start();
        var port = ((IPEndPoint)l.LocalEndpoint).Port;
        l.Stop();
        return port;
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

file static class KestrelHelpers
{
    public static WebApplicationBuilder WithKestrelOnLoopback(this WebApplicationBuilder builder, int port)
    {
        builder.WebHost.UseKestrel()
                       .UseUrls($"http://127.0.0.1:{port}");
        return builder;
    }
}
