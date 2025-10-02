


using HBRAK.Frontier.Authorization.Service;

IAuthorizationService auth = new AuthorizationService("9d9462d1-7830-459e-9317-c0a8ce3f8c8d");


try
{
    await auth.LoadAndRefreshAllAsync();
}
catch(Exception ex)
{
    Console.WriteLine("Error during loading and refreshing tokens: " + ex.Message);
}

if (auth.Tokens.Count == 0)
{
    Console.WriteLine("No tokens found, please add a token from website cookie.");

    string? code = Console.ReadLine();
    if (string.IsNullOrWhiteSpace(code))
    {
        Console.WriteLine("No token provided, exiting.");
        return;
    }
    auth.AddTokenFromWebsiteCookie(code);
}

foreach (var token in auth.Tokens)
{
    Console.WriteLine($"Token for {token.EveSub} (expires at {token.ExpiresAt}):");
}

Console.WriteLine("Refresh tokens? Y/N");
var key = Console.ReadKey();
Console.WriteLine("\n");
if (key.Key == ConsoleKey.Y)
{
    foreach (var token in auth.Tokens)
    {
        try
        {
            var refreshed = await auth.RefreshAsync(token);
            Console.WriteLine($"Refreshed token for {token.EveSub}, new expiry at {refreshed.ExpiresAt}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error refreshing token for {token.EveSub}: " + ex.Message);
        }
    }
}
else
{
    Console.WriteLine("Exiting without refreshing.");
}


