using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace HBRAK.Frontier.Api.Data.Meta.Config;

public class ConfigRpcEndpoint
{
    [JsonPropertyName("http")]
    public string Http { get; set; } = string.Empty;

    [JsonPropertyName("webSocket")]
    public string WebSocket { get; set; } = string.Empty;
}
