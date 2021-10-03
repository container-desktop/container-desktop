using ContainerDesktop.Common;
using ContainerDesktop.Common.Services;
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
            .UseSerilog((_, config) =>
            {
                config.WriteTo.Conditional(e => e.Level <= Serilog.Events.LogEventLevel.Warning, x => x.EventLog(Product.InstallerDisplayName, manageEventSource: true));
            });

    static void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<App>();
        services.AddSingleton<IApplicationContext>(sp => sp.GetRequiredService<App>());
        services.AddSingleton<MainWindow>();
        services.AddSingleton<MainViewModel>();
        services.AddSingleton<IWslService, WslService>();
        services.AddSingleton<IProcessExecutor, ProcessExecutor>();
        services.AddSingleton<IContainerEngine, DefaultContainerEngine>();
        services.AddTransient<IFileSystem, FileSystem>();
        services.AddSingleton<IConfigurationService, ConfigurationService>();
    }
}
