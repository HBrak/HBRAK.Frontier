﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace HBRAK.Frontier.Api.Data.Chain.SmartAssemblies;

public class SmartAssemblyNetworkNode : SmartAssemblyBase
{
    [JsonPropertyName("networkNode")] public NetworkNodeInfo NetworkNode { get; set; } = new();
}
