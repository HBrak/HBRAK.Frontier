using HBRAK.Frontier.Api.Data.Info;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace HBRAK.Frontier.Api.Data.Chain.SmartAssemblies;

public class SmartAssemblyStorageUnit : SmartAssemblyBase
{
    [JsonPropertyName("storage")] public StorageInfo Storage { get; set; } = new();
}
