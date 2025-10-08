using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HBRAK.Frontier.Authorization.TokenStore.Service;

public class WindowsDpapiTokenStoreOptions
{
    public string TokenStoragePath { get; set; } = "%localappdata%/HBRAK.Frontier/Auth";
}
