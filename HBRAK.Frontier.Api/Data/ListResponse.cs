using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace HBRAK.Frontier.Api.Data;

public class ListResponse
{
    [JsonPropertyName("data")]
    public JsonElement Data { get; set; }

    [JsonPropertyName("metadata")]
    public ListMetaData MetaData { get; set; } = new();
}
