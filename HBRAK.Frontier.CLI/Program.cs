using HBRAK.Frontier.Api.Data.Chain.SmartAssemblies;
using HBRAK.Frontier.Api.Data.Game.Fuels;
using HBRAK.Frontier.Api.Service;
using HBRAK.Frontier.Authorization.Data;
using HBRAK.Frontier.Authorization.Service;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HBRAK.Frontier.Cli
{
    internal static class Program
    {
        private static readonly Random Rng = new();

        public static async Task Main(string[] args)
        {
            IAuthorizationService auth = new AuthorizationService("9d9462d1-7830-459e-9317-c0a8ce3f8c8d");

            try
            {
                await auth.LoadAndRefreshAllAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error during loading and refreshing tokens: " + ex.Message);
            }

            if (auth.Tokens.Count == 0)
            {
                Console.WriteLine("No tokens found, please add a token from website cookie.");
                Console.Write("Paste token: ");
                string? code = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(code))
                {
                    Console.WriteLine("No token provided, exiting.");
                    return;
                }
                await auth.AddTokenFromWebsiteCookie(code);
            }

            foreach (var token in auth.Tokens)
            {
                Console.WriteLine($"Token for {token.EveSub} (expires at {token.ExpiresAt})");
            }

            // Use the first token for endpoints that require auth (e.g., jumps)
            AccessToken? accessToken = auth.Tokens.FirstOrDefault();
            accessToken = await auth.RefreshAsync(accessToken);

            IApiService api = new ApiService();

            // Config/health endpoints

            await RunConfigTests(api);

            // --------- Chain endpoints ---------
            await RunListTest(
                "Killmails",
                () => api.GetKillMailsAsync(100),
                id => api.GetKillMailIdAsync(id),
                k => $"{k?.Id}: {k?.Killer?.Name} -> {k?.Victim?.Name}");

            await RunListTest(
                "Smart Characters",
                () => api.GetSmartCharactersAsync(100),
                id => api.GetSmartCharacterIdAsync(id),
                c => $"{c?.Name} ({c?.Id})");

            // --------- Game endpoints ---------
            await RunListTest(
                "Fuels",
                () => api.GetFuelsAsync(100),
                // no single-by-id endpoint for fuels; keep a no-op for the second call
                id => Task.FromResult<FuelType?>(null),
                f => $"{f?.Type?.Name} efficiency={f?.Efficiency}");

            if (accessToken is not null)
            {
                await RunListTest(
                    "Smart Character Jumps",
                    () => api.GetSmartCharacterJumpsAsync(accessToken, 100),
                    id => api.GetSmartCharacterJumpIdAsync(id, accessToken),
                    j => $"{j?.Ship?.TypeId} from {j?.Origin?.Name} → {j?.Destination?.Name} at {j?.Time}");
            }
            else
            {
                Console.WriteLine("No access token available for Smart Character Jumps; skipping.");
            }

            await RunListTest(
                "Solar Systems",
                () => api.GetSolarSystemsAsync(100),
                id => api.GetSolarSystemIdAsync(id),
                s => $"{s?.Name} (region {s?.RegionId})");

            await RunListTest(
                "Tribes",
                () => api.GetTribesAsync(100),
                id => api.GetTribeIdAsync(id),
                t => $"{t?.Name} [{t?.NameShort}] members={t?.MemberCount}");

            await RunListTest(
                "Types",
                () => api.GetTypesAsync(100),
                id => api.GetTypeIdAsync(id),
                t => $"{t?.Name} (cat {t?.CategoryName})");

            await RunSmartAssembliesByTypeAsync(api, 100);
        }

        private static async Task RunConfigTests(IApiService api)
        {
            Console.WriteLine("=== Config/Health ===");

            var abis = await api.GetAbisConfigAsync();
            Console.WriteLine($"ABIs: {abis is not null}");

            var config = await api.GetConfigAsync();
            foreach (var c in config ?? [])
            {
                Console.WriteLine($"Config name={c.Name}, chain={c.ChainId}");
            }

            var health = await api.GetHealthAsync();
            Console.WriteLine($"Health ok={health?.Ok}");

            Console.WriteLine();
        }

        private static async Task RunListTest<TList, TSingle>(
            string label,
            Func<Task<List<TList>>> listFunc,
            Func<string, Task<TSingle?>> singleFunc,
            Func<TSingle?, string> displayFunc)
            where TList : class
            where TSingle : class
        {
            Console.WriteLine($"=== {label} ===");

            var list = await WithLoadingDots(() => listFunc());
            Console.WriteLine($"Got {list.Count} {label}");

            if (list.Count > 0)
            {
                var random = list[Rng.Next(list.Count)];
                var idProp = typeof(TList).GetProperty("Id") ?? typeof(TList).GetProperty("id");
                var id = idProp?.GetValue(random)?.ToString();

                if (!string.IsNullOrEmpty(id))
                {
                    var single = await singleFunc(id);
                    if (single is not null)
                    {
                        Console.WriteLine($"Random single picked: {displayFunc(single)}");
                    }
                }
                else
                {
                    Console.WriteLine("Could not determine ID property.");
                }
            }

            Console.WriteLine();
        }

        private static async Task RunSmartAssembliesByTypeAsync(IApiService api, int limit = 100)
        {
            Console.WriteLine("=== Smart Assemblies ===");

            // 1) Get a larger list so we likely have at least one of each type
            var list = await WithLoadingDots(() => api.GetSmartAssembliesAsync(null, limit));

            if (list is null || list.Count == 0)
            {
                Console.WriteLine("No smart assemblies returned.");
                Console.WriteLine();
                return;
            }

            // 2) Group by Type (works if Type is enum or string)
            var groups = list
                .GroupBy(sa => sa?.Type?.ToString() ?? "Unknown")
                .OrderBy(g => g.Key)
                .ToList();

            // 3) Pick one random from each group
            var picks = new List<SmartAssemblyReference>();
            foreach (var g in groups)
            {
                var arr = g.Where(x => x is not null).ToList();
                if (arr.Count == 0) continue;
                var pick = arr[Rng.Next(arr.Count)];
                picks.Add(pick);
            }

            // 4) Fetch details for each pick and print a summary line
            foreach (var pick in picks)
            {
                // Safeguard: get Id property as string
                var id = pick.Id?.ToString();
                if (string.IsNullOrWhiteSpace(id))
                {
                    Console.WriteLine($"[Skip] Missing Id for type {pick.Type}");
                    continue;
                }

                SmartAssemblyBase? full = null;
                try
                {
                    full = await api.GetSmartAssemblyIdAsync(id);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Error] Fetching assembly {id} ({pick.Type}): {ex.Message}");
                }

                // Compose a friendly line regardless of concrete runtime type
                var typeText = full?.Type.ToString() ?? pick.Type?.ToString() ?? "Unknown";
                string name = full?.Name ?? pick.Name;
                if (string.IsNullOrEmpty(name))
                {
                    name = "(unnamed)";
                }
                string sys = full?.SolarSystem?.Name ?? pick.SolarSystem?.Name ?? string.Empty;
                if (string.IsNullOrEmpty(name))
                {
                    name = "(no system)";
                }
                Console.WriteLine($"• {typeText,-18} {name}  —  {sys}  (id {id})");
            }

            Console.WriteLine();
        }

        private static async Task<T> WithLoadingDots<T>(Func<Task<T>> action, string message = "Loading, this may take a while")
        {
            using var cts = new CancellationTokenSource();
            var token = cts.Token;

            var loader = Task.Run(async () =>
            {
                int dots = 0;
                while (!token.IsCancellationRequested)
                {
                    dots = (dots % 10) + 1;
                    var text = $"{message}{new string('.', dots)}";
                    Console.Write($"\r{text}   "); // overwrite line
                    await Task.Delay(500, token).ContinueWith(_ => { });
                }
            }, token);

            try
            {
                var result = await action();
                return result;
            }
            finally
            {
                cts.Cancel();
                // clear the line
                Console.Write("\r" + new string(' ', Console.WindowWidth - 1) + "\r");
            }
        }


    }
}

