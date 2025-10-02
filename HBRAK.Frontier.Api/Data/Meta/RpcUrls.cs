using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace HBRAK.Frontier.Api.Data.Meta;

public class RpcUrls
{
    [JsonPropertyName("public")]
    public List<string> Public { get; set; } = new();

    [JsonPropertyName("private")]
    public List<string> Private { get; set; } = new();
}
