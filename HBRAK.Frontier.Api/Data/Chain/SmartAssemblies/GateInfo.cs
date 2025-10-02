using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace HBRAK.Frontier.Api.Data.Chain.SmartAssemblies;

public class GateInfo : SimpleNodeInfo
{
    [JsonPropertyName("destinationId")] public string DestinationId { get; set; } = string.Empty;
    [JsonPropertyName("inRange")] public List<SmartAssemblyReference> InRange { get; set; } = new();
    [JsonPropertyName("linked")] public bool Linked { get; set; }
}
