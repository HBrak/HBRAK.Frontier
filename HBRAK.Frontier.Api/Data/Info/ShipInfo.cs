using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace HBRAK.Frontier.Api.Data.Info;

public class ShipInfo
{
    [JsonPropertyName("instanceId")]
    public long InstanceId { get; set; }

    [JsonPropertyName("typeId")]
    public int TypeId { get; set; }
}
