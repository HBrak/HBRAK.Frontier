using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace HBRAK.Frontier.Api.Data.Chain;

public class SimpleNode
{
    [JsonPropertyName("isParentNodeOnline")] public bool IsParentNodeOnline { get; set; }
}

