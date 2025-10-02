using HBRAK.Frontier.Authorization.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HBRAK.Frontier.Api.Service;

public class ApiService
{
    private HttpClient _http;
    private AccessToken _token;

    public ApiService()
    {
        _http = new HttpClient
        {
            BaseAddress = new Uri("https://world-api-stillness.live.tech.evefrontier.com")
        };
    }
}
