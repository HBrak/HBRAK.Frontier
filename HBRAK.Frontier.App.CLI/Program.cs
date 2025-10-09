using HBRAK.Frontier.Authorization.Api.Data;
using HBRAK.Frontier.Authorization.Api.Service;
using HBRAK.Frontier.Authorization.Service;
using HBRAK.Frontier.CLI;
using HBRAK.Frontier.Communication.Api.Data.Chain.Enums;
using HBRAK.Frontier.Communication.Api.Data.Chain.SmartAssemblies;
using HBRAK.Frontier.Communication.Api.Data.Info;
using HBRAK.Frontier.Communication.Api.Service;
using HBRAK.Frontier.Communication.Chain.Service;
using HBRAK.Frontier.Communication.Chain.Tools;
using HBRAK.Frontier.Database.Indexer.Raw;
using HBRAK.Frontier.Database.Indexer.Raw.Context;
using HBRAK.Frontier.Database.Indexer.Raw.Evm;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.IO;

namespace HBRAK.Frontier.Cli;

internal static class Program
{
    private static readonly Random Rng = new();
    public static async Task Main(string[] args)
    {
        var builder = Host.CreateApplicationBuilder(args);
        builder.Configuration
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);

        builder.Services.Configure<ApiServiceOptions>(
            builder.Configuration.GetSection("Api"));
        builder.Services.Configure<AuthorizationServiceOptions>(
            builder.Configuration.GetSection("Authorization"));
        builder.Services.Configure<ChainServiceOptions>(
            builder.Configuration.GetSection("Chain"));

        builder.Logging.AddConsole();
        builder.Logging.AddDebug();

        builder.Services.AddDbContextPool<FrontierRawDb>(o =>
                o.UseSqlite($"Data Source={DefaultSqlitePath()}"));

        builder.Services.AddSingleton<IChainService, EvmChainService>();
        builder.Services.AddSingleton<IAuthorizationService, AuthorizationService>();
        builder.Services.AddSingleton<ITokenStore, WindowsDpapiTokenStore>();
        builder.Services.AddSingleton<IApiService, ApiService>();
        builder.Services.AddSingleton<IChainContracts, EvmChainContractsService>();

        builder.Services.AddSingleton<IRawIndexer, EvmRawIndexer>();
        builder.Services.AddHostedService<RawIngestHostedService>();



        var host = builder.Build();

        await using (var scope = host.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<FrontierRawDb>();

            // 1) Ensure directory exists (SQLite won’t create nested folders)
            var cs = db.Database.GetDbConnection().ConnectionString;
            var dataSource = new SqliteConnectionStringBuilder(cs).DataSource;
            var dir = Path.GetDirectoryName(Path.GetFullPath(dataSource));
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir!);

            // 2) Create database file + tables from CURRENT MODEL (no migrations)
            //    Use the relational creator for a stronger "create everything" behavior.
            var creator = db.GetService<Microsoft.EntityFrameworkCore.Storage.IRelationalDatabaseCreator>();

            // Create database file if missing
            if (!await creator.ExistsAsync())
                await creator.CreateAsync();

            // Create tables if missing (idempotent: will skip those that already exist)
            await creator.CreateTablesAsync();

            // 3) (Optional) sanity log
            await LogTablesAsync(db);
        }

        await host.RunAsync();



        var auth = host.Services.GetRequiredService<IAuthorizationService>();
        var chain = host.Services.GetRequiredService<IChainService>();
        var contracts = host.Services.GetRequiredService<IChainContracts>();
        var api = host.Services.GetRequiredService<IApiService>();



        return;

        var bridge = new VaultSignIn();
        var signed = await bridge.RunAsync(expectedChainId: 695569, ct: new());
        var address = signed?.Address ?? null;

        if (string.IsNullOrWhiteSpace(address))
        {
            Console.WriteLine("No address obtained from signing, exiting.");
            return;
        }

        var character = await api.GetSmartCharacterAdressAsync(address);
        Console.WriteLine($"Logged into vault as: {character!.Name} - {character.Address}");

        await TestAuth(auth);

        return;

        var tribes = await api.GetTribesAsync();
        var TribeRef = tribes.FirstOrDefault(c => c.Name == "Wandering Order of the Last Frontier");
        var targetTribe = await api.GetTribeIdAsync(TribeRef.Id.ToString());

        foreach (var member in targetTribe.Members)
        {
            await GetAssets(api, member.Address);
        }

        return;
    }

    static string DefaultSqlitePath()
    {
        var dataDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "HBRAK.Frontier", "Db");
        Directory.CreateDirectory(dataDir);
        return Path.Combine(dataDir, "frontier_raw.db");
    }

    static async Task LogTablesAsync(DbContext db)
    {
        Console.WriteLine("EF model tables:");
        foreach (var et in db.Model.GetEntityTypes())
            Console.WriteLine($" - {et.GetSchema() ?? "main"}.{et.GetTableName()}");

        Console.WriteLine("SQLite actual tables:");
        var rows = await db.Database
            .SqlQueryRaw<string>("SELECT name FROM sqlite_master WHERE type='table' ORDER BY name;")
            .ToListAsync();
        foreach (var t in rows) Console.WriteLine($" * {t}");
    }

    public static async Task GetAssets(IApiService api, string characterId)
    {
        var characterData = await api.GetSmartCharacterAdressAsync(characterId);
        if (characterData is null)
        {
            Console.WriteLine($"Character with id {characterId} not found");
            return;
        }

        var smartAssemblies = characterData?.SmartAssemblies.OrderBy(assembly => assembly.SolarSystem.Id).ToList();

        Console.WriteLine($"------ {characterData!.Name} ------------------------------");
        Console.WriteLine($"{characterData.Name} has {smartAssemblies?.Count ?? 0} smart assemblies");

        Console.WriteLine($"-- {smartAssemblies?.Where(a => a.Type == SmartAssemblyType.SmartStorageUnit.ToString()).Count()} SmartStorageUnit");
        Console.WriteLine($"-- {smartAssemblies?.Where(a => a.Type == SmartAssemblyType.SmartGate.ToString()).Count()} SmartGate");
        Console.WriteLine($"-- {smartAssemblies?.Where(a => a.Type == SmartAssemblyType.SmartTurret.ToString()).Count()} SmarSmartTurrettGate");
        Console.WriteLine($"-- {smartAssemblies?.Where(a => a.Type == SmartAssemblyType.Manufacturing.ToString()).Count()} Manufacturing");
        Console.WriteLine($"-- {smartAssemblies?.Where(a => a.Type == SmartAssemblyType.Refinery.ToString()).Count()} Refinery");
        Console.WriteLine($"-- {smartAssemblies?.Where(a => a.Type == SmartAssemblyType.NetworkNode.ToString()).Count()} NetworkNode");
        Console.WriteLine($"-- {smartAssemblies?.Where(a => a.Type == SmartAssemblyType.SmartHangar.ToString()).Count()} SmartHangar");

        if (characterData.Name == "Paarth")
        {
            Console.WriteLine($"**** skipping details because Paarth just takes too long");
            return;
        }

        foreach (var smartRef in smartAssemblies ?? [])
        {
            var smartThing = await api.GetSmartAssemblyIdAsync(smartRef.Id);

            var TypeId = smartThing?.TypeId ?? 0;
            var type = await api.GetTypeIdAsync(TypeId.ToString());

            Console.WriteLine($"{smartThing!.SolarSystem.Name}: State({smartThing.State}), {type!.Name}, {type.CategoryName} '{smartThing.Name}' {smartThing.Id}");

            switch (smartThing)
            {
                case SmartAssemblyGate thing:

                    if (string.IsNullOrWhiteSpace(thing.Gate.DestinationId)){
                        Console.WriteLine($"-- Not linked");
                        break;
                    }

                    var toGate = await api.GetSmartAssemblyIdAsync(thing.Gate.DestinationId) as SmartAssemblyGate;
                    Console.WriteLine($"-- Linked:({toGate!.Gate.Linked}), To: {toGate.SolarSystem}");
                    break;
                case SmartAssemblyNetworkNode thing:
                    Console.WriteLine($"-- Burning:({thing.NetworkNode.Burn.IsBurning}), EnergyReserve: {thing.NetworkNode.TotalReservedEnergy}");
                    break;
                case SmartAssemblyStorageUnit thing:
                    foreach (var item in thing.Storage.MainInventory.Items)
                    {
                        Console.WriteLine($"-- Item: {item.Name}, Amount: {item.Quantity}");
                    }
                    break;
                default:
                    break;
            }
        }
    }

    public static async Task TestChain(IApiService api, IChainService chain, IChainContracts contracts)
    {
        Console.WriteLine("=== Testing Chain ===");

        Console.WriteLine("Getting smart characters");
        var chars = await api.GetSmartCharactersAsync();
        var hoelbrakRef = chars.FirstOrDefault(c => c.Name == "Hoelbrak");
        var hoelbrak = await api.GetSmartCharacterAdressAsync(hoelbrakRef?.Address ?? string.Empty);


        Console.WriteLine("Getting all smart gate references");
        var gatesRef = await api.GetSmartAssembliesAsync(SmartAssemblyType.SmartGate);

        Console.WriteLine("Getting full gate info per reference");
        List<SmartAssemblyGate> gates = [];
        foreach (var gateRef in gatesRef)
        {
            var gate = await api.GetSmartAssemblyIdAsync(gateRef.Id) as SmartAssemblyGate;
            if (gate is null) continue;
            gates.Add(gate);
        }

        var userId = HexStringExtensions.ToBigInt(hoelbrak!.Id);
        if (userId is null)
        {
            Console.WriteLine($"Error in config. {hoelbrak.Name} (userId is null)");
        }


        foreach (var gate in gates)
        {
            if (userId is null) break;

            var from = HexStringExtensions.ToBigInt(gate.Id)!;
            SmartAssemblyGate? toGate = gates.FirstOrDefault(g => g.Gate.DestinationId == gate.Id);
            var to = HexStringExtensions.ToBigInt(toGate?.Id);

            if (to is null | toGate is null)
            {
                Console.WriteLine($"Error: *Gate {gate.Id}, in: '{gate.SolarSystem.Name}' has no destination set*");
                continue;
            }

            bool isOnline = await contracts.SystemViewAsync<bool>("SmartGateSystem", "areGatesOnline", [from, to!]);
            if (!isOnline)
            {
                Console.WriteLine($"Gate {gate.Id}, From:'{gate.SolarSystem.Name}', To:'{toGate!.SolarSystem.Name}' is {(isOnline ? "ONLINE" : "OFFLINE")}");
                continue;
            }

            bool canJump = await contracts.SystemViewAsync<bool>("SmartGateSystem", "canJump", [userId, from, to!]);
            Console.WriteLine($"Character: '{hoelbrak.Name}' {(canJump ? "CAN" : "CAN NOT")} jump from Gate '{gate.SolarSystem.Name}' to''{toGate!.SolarSystem.Name}'");
        }
    }

    public static async Task FindSpecificTypeNameInStorages(string name, IApiService api)
    {
        Console.WriteLine("=== Api function chaining ===");

        List<SmartAssemblyStorageUnit> storages = [];

        Console.WriteLine($"Fetching specific types (Name = {name}) in storages");
        var types = await api.GetTypesAsync(100); // adjust if you need more than 100
        var idSet = types
            .Where(t => t.Name.Contains(name, StringComparison.OrdinalIgnoreCase))
            .Select(t => t.Id)
            .ToHashSet();

        Console.WriteLine($"Found {idSet.Count} types with name: {name}");
        if (idSet.Count == 0) return;

        Console.WriteLine("Fetching all storage refs");
        var storageRefs = await api.GetSmartAssembliesAsync(SmartAssemblyType.SmartStorageUnit);

        // 1) De-duplicate refs by Id to avoid fetching the same storage multiple times
        var distinctRefs = storageRefs
            .DistinctBy(r => r.Id)
            .ToList();

        int current = 0;
        foreach (var storageRef in distinctRefs)
        {
            Console.Write($"\rFetching storage by reference {current}/{distinctRefs.Count}");
            var storage = await api.GetSmartAssemblyIdAsync(storageRef.Id) as SmartAssemblyStorageUnit;
            if (storage is not null)
                storages.Add(storage);
            current++;
        }
        Console.WriteLine();

        // 2) De-duplicate storages by Id (belt & suspenders)
        storages = storages
            .DistinctBy(s => s.Id)
            .ToList();

        // 3) Filter to storages that have at least one matching type,
        //    and compute per-type totals to avoid duplicate prints
        var storagesWithType = storages
            .Select(s => new
            {
                Storage = s,
                PerType = (s?.Storage?.MainInventory?.Items ?? Enumerable.Empty<InventoryItem>())
                    .Where(i => idSet.Contains(i.TypeId))
                    .GroupBy(i => i.TypeId)
                    .Select(g => new
                    {
                        TypeId = g.Key,
                        Name = g.First().Name,
                        Qty = g.Sum(x => x.Quantity)
                    })
                    .ToList()
            })
            .Where(x => x.PerType.Count > 0)
            // Optional: sort storages by total qty of matching items
            .OrderByDescending(x => x.PerType.Sum(t => t.Qty))
            .ToList();

        Console.WriteLine($"{storagesWithType.Count} storages found with {name}");

        // 4) Print once per storage per type (no duplicates)
        foreach (var x in storagesWithType)
        {
            var owner = x.Storage.Owner?.Name ?? "<unknown>";
            var system = x.Storage.SolarSystem?.Name ?? "<unknown>";
            var storageName = x.Storage.Name ?? string.Empty;
            var storageId = x.Storage.Id;

            foreach (var t in x.PerType)
            {
                Console.WriteLine($"{owner} has {t.Qty} {t.Name} stored in {system} - {storageName} - {storageId}");
            }
        }
    }

    public static async Task TestAuth(IAuthorizationService auth)
    {
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
        ApiToken? accessToken = auth.Tokens.FirstOrDefault();
        accessToken = await auth.RefreshAsync(accessToken!);
    }
    public static async Task TestApi(ApiToken? accessToken, IApiService api)
    {
        // Config/health endpoints
        await RunConfigTests(api);

        // --------- Chain endpoints ---------
        await RunListTest(
            "Killmails",
            () => api.GetKillMailsAsync(100),
            id => api.GetKillMailIdAsync(id),
            k => $"{k?.Id}: {k?.Killer?.Name} --killed-> {k?.Victim?.Name}");

        await RunListTestCharacters(
            "Smart Characters",
            () => api.GetSmartCharactersAsync(100),
            adress => api.GetSmartCharacterAdressAsync(adress),
            c => $"{c?.Name ?? "(no name)"} ({c?.Id})");

        // --------- Game endpoints ---------
        await RunListTestFuel(
            "Fuels",
            () => api.GetFuelsAsync(100),
            f => $"{f?.Type?.Name} efficiency={f?.Efficiency}");

        if (accessToken is not null)
        {
            await RunListTest(
                "Smart Character Jumps",
                () => api.GetSmartCharacterJumpsAsync(accessToken, 100),
                id => api.GetSmartCharacterJumpIdAsync(id, accessToken),
                j => $"{j?.Ship?.TypeId} from {j?.Origin?.Name} --jump-> {j?.Destination?.Name} at {j?.Time}");
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
            var idProp = typeof(TList).GetProperty("Id");
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

    private static async Task RunListTestCharacters<TList, TSingle>(
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
            var adressProp = typeof(TList).GetProperty("Address");
            var adress = adressProp?.GetValue(random)?.ToString();

            if (!string.IsNullOrEmpty(adress))
            {
                var single = await singleFunc(adress);
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

    private static async Task RunListTestFuel<TSingle>(
    string label,
    Func<Task<List<TSingle>>> listFunc,
    Func<TSingle?, string> displayFunc)
    where TSingle : class
    {
        Console.WriteLine($"=== {label} ===");

        var list = await WithLoadingDots(() => listFunc());
        Console.WriteLine($"Got {list.Count} {label}");

        if (list.Count > 0)
        {
            TSingle random = list[Rng.Next(list.Count)];
            Console.WriteLine($"Random single picked: {displayFunc(random)}");
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

