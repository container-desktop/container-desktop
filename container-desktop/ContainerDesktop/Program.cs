using ContainerDesktop.Common;
using ContainerDesktop.Services;
using ContainerDesktop.ViewModels;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using System.IO.Abstractions;

namespace ContainerDesktop;

static class Program
{
    [STAThread]
    static int Main(string[] args)
    {
        var hostBuilder = CreateHostBuilder(args);
        using var host = hostBuilder.Build();
        host.Start();
        var app = host.Services.GetRequiredService<App>();
        app.InitializeComponent();
        return app.Run();
    }

    static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureAppConfiguration(builder =>
            {
                builder
                    .AddJsonFile("appsettings.json", true)
                    .AddUserSecrets<App>()
                    .AddEnvironmentVariables("CONTAINERDESKTOP_");
            })
            .ConfigureServices(ConfigureServices)
            .UseConsoleLifetime(x => x.SuppressStatusMessages = true)
            .UseSerilog((_, sp, config) =>
            {
                var productInfo = sp.GetRequiredService<IProductInformation>();
                config.WriteTo.File(Path.Combine(productInfo.ContainerDesktopAppDataDir, "logs", "log.txt"), fileSizeLimitBytes: 1024 * 1024 * 10, rollOnFileSizeLimit: true);
                config.WriteTo.Conditional(e => e.Level <= Serilog.Events.LogEventLevel.Warning, x => x.EventLog(productInfo.DisplayName));
            });

    static void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<App>();
        services.AddSingleton<IApplicationContext>(sp => sp.GetRequiredService<App>());
        services.AddSingleton<MainWindow>();
        services.AddSingleton<MainViewModel>();
        services.AddCommon();
        services.AddWsl();
        services.AddProcessExecutor();
        services.AddSingleton<IContainerEngine, DefaultContainerEngine>();
        services.AddSingleton<IConfigurationService, ConfigurationService>();
    }
}
