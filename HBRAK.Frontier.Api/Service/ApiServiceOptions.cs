using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Net.WebRequestMethods;

namespace HBRAK.Frontier.Api.Service;

public class ApiServiceOptions
{
    public string BaseUrl { get; set; } = "https://world-api-stillness.live.tech.evefrontier.com";
    public int DefaultLimit { get; set; } = 100;
    public int MaxLimit { get; set; } = 100;
    public int TimeoutSeconds { get; set; } = 30;
    public string EndpointAbisConfig { get; set; } = "abis/config";
    public string EndpointConfig { get; set; } = "config";
    public string EndpointHealth { get; set; } = "health";
    public string EndpointKillMails { get; set; } = "v2/killmails";
    public string EndpointSmartAssemblies { get; set; } = "v2/smartassemblies";
    public string EndpointSmartCharacters { get; set; } = "v2/smartcharacters";
    public string EndpointFuels { get; set; } = "v2/fuels";
    public string EndpointSmartCharacterJumps { get; set; } = "v2/smartcharacters/me/jumps";
    public string EndpointSolarSystems { get; set; } = "v2/solarsystems";
    public string EndpointTribes { get; set; } = "v2/tribes";
    public string EndpointTypes { get; set; } = "v2/types";
}
