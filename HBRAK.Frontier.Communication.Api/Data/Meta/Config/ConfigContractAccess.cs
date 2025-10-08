using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace HBRAK.Frontier.Communication.Api.Data.Meta.Config;

public class ConfigContractAccess
{
    [JsonPropertyName("address")]
    public string Address { get; set; } = string.Empty;
}
