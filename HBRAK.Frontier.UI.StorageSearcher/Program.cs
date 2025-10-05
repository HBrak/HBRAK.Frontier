using Avalonia;
using HBRAK.Frontier.Api.Service;
using HBRAK.Frontier.Authorization.Service;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;

namespace HBRAK.Frontier.UI.StorageSearcher
{
    internal sealed class Program
    {
        // Initialization code. Don't use any Avalonia, third-party APIs or any
        // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
        // yet and stuff might break.
        [STAThread]
        public static void Main(string[] args)
        {
            var builder = Host.CreateApplicationBuilder(args);

            //config file
            builder.Configuration
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);

            //options
            builder.Services.Configure<ApiServiceOptions>(
                builder.Configuration.GetSection("Api"));
            builder.Services.Configure<AuthorizationServiceOptions>(
                builder.Configuration.GetSection("Authorization"));

            //loggers
            builder.Logging.AddConsole();
            builder.Logging.AddDebug();

            //services
            builder.Services.AddSingleton<IAuthorizationService, AuthorizationService>();
            builder.Services.AddSingleton<ITokenStore, WindowsDpapiTokenStore>();
            builder.Services.AddSingleton<IApiService, ApiService>();

            //main window
            builder.Services.AddSingleton<Views.MainWindow>();
            builder.Services.AddSingleton<ViewModels.MainWindowViewModel>();

            var host = builder.Build();

            BuildAvaloniaApp(host.Services).StartWithClassicDesktopLifetime(args);
        }





        // Avalonia configuration, don't remove; also used by visual designer.
        public static AppBuilder BuildAvaloniaApp(IServiceProvider services)
            => AppBuilder.Configure(() => new App(services))
                         .UsePlatformDetect()
                         .WithInterFont()
                         .LogToTrace();
    }
}
