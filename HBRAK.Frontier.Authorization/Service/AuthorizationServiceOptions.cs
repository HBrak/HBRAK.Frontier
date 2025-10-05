using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HBRAK.Frontier.Authorization.Service;

public class AuthorizationServiceOptions
{
    public string AuthUrl { get; set; } = "https://auth.evefrontier.com/oauth2/token";
    public string AppId { get; set; } = "9d9462d1-7830-459e-9317-c0a8ce3f8c8d";
    public string TokenStoragePath { get; set; } = "%localappdata%/HBRAK.Frontier/Auth";
}
