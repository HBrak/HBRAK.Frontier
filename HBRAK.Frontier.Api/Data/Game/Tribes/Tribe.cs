using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace HBRAK.Frontier.Api.Data.Game.Tribes;

public class Tribe : TribeReference
{
    [JsonPropertyName("members")]
    public List<SmartCharacterReference> Members { get; set; } = new();
}
