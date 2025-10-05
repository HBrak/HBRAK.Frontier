using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HBRAK.Frontier.Api.Data.Chain.Enums;
using HBRAK.Frontier.Api.Data.Chain.SmartAssemblies;
using HBRAK.Frontier.Api.Data.Game.Type;
using HBRAK.Frontier.Api.Data.Info;
using HBRAK.Frontier.Api.Service;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace HBRAK.Frontier.App.StorageSearcher.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly IApiService _api;

    private List<SmartAssemblyStorageUnit> _storages = [];
    private List<TypeDetails> _allTypes = [];

    [ObservableProperty] private int _storageCount;
    [ObservableProperty] private string _targetType = string.Empty;
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private string _loadingText = string.Empty;
    [ObservableProperty] private ObservableCollection<string> _targetTypesStored = [];

    public MainWindowViewModel(IApiService api)
    {
        _api = api;
        _ = LoadStorages();
        _ = LoadTypes();
    }

    [RelayCommand]
    public void FindTargetTypes()
    {
        TargetTypesStored = [];

        var idSet = _allTypes
            .Where(t => t.Name.Contains(TargetType, StringComparison.OrdinalIgnoreCase))
            .Select(t => t.Id)
            .ToHashSet();

        TargetTypesStored.Add($"Found {idSet.Count} types with name: {TargetType}");

        Console.WriteLine($"Found {idSet.Count} types with name: {TargetType}");
        if (idSet.Count == 0) return;

        var storagesWithType = _storages
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
        .OrderByDescending(x => x.PerType.Sum(t => t.Qty))
        .ToList();

        Console.WriteLine($"{storagesWithType.Count} storages found with {TargetType}");

        if (storagesWithType.Count == 0)
        {
            TargetTypesStored.Add("No storages found with that type.");
            return;
        }
        TargetTypesStored.Clear();

        foreach (var x in storagesWithType)
        {
            var owner = x.Storage.Owner?.Name ?? "<unknown>";
            var system = x.Storage.SolarSystem?.Name ?? "<unknown>";
            var storageName = x.Storage.Name ?? string.Empty;
            var storageId = x.Storage.Id;

            foreach (var t in x.PerType)
            {
                string line = $"{owner} has {t.Qty} {t.Name} stored in {system} - {storageName} - {storageId}";
                TargetTypesStored.Add(line);
                Console.WriteLine(line);
            }
        }
    }

    [RelayCommand]
    public async Task LoadTypes()
    {
        _allTypes = await _api.GetTypesAsync(limit: 1000);
    }

    [RelayCommand(AllowConcurrentExecutions = false)]
    public async Task LoadStorages()
    {
        if (IsLoading) return;

        try
        {
            IsLoading = true;

            List<SmartAssemblyStorageUnit> loadingStorages = [];
            int loadingCount = 0;

            LoadingText = "Fetching all storage refs";

            Console.WriteLine(LoadingText);
            var storageRefs = await _api.GetSmartAssembliesAsync(SmartAssemblyType.SmartStorageUnit);

            LoadingText = $"Loading Storages! Total: {storageRefs.Count}";
            Console.WriteLine(LoadingText);

            Console.WriteLine();

            foreach (var storageRef in storageRefs)
            {
                LoadingText = $"Fetching storage by reference {loadingCount}/{storageRefs.Count}   ";
                Console.Write(LoadingText);
                var storage = await _api.GetSmartAssemblyIdAsync(storageRef.Id) as SmartAssemblyStorageUnit;
                if (storage is not null)
                    loadingStorages.Add(storage);
                loadingCount++;
            }
            Console.WriteLine();

            _storages = loadingStorages
                .DistinctBy(s => s.Id)
                .ToList();

            StorageCount = loadingCount;
        }
        finally
        {
            LoadingText = string.Empty;
            IsLoading = false;
        }

        
    }
}
