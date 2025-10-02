using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace HBRAK.Frontier.Api.Data.Meta.Health;

public class HealthResponse
{
    [JsonPropertyName("ok")]
    public bool Ok { get; set; }
}
