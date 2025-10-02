using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace HBRAK.Frontier.Api.Data.Meta.Config;

public class ConfigItemTypeIDs
{
    [JsonPropertyName("fuel")]
    public int Fuel { get; set; }
}
