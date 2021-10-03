using ContainerDesktop.Common;
using ContainerDesktop.Common.DesiredStateConfiguration;
using ContainerDesktop.Common.Services;
using ContainerDesktop.Installer.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Serilog;
using System.IO.Abstractions;
using System.Windows.Forms;

namespace ContainerDesktop.Installer;

static class Program
{
    
    [STAThread]
    static int Main(string[] args)
    {
        var options = InstallerOptions.ParseOptions(args);
        var unattendedWithConsole = options.Unattended && (PInvoke.Kernel32.AttachConsole(-1 /* ATTACH_PARENT_PROCESS */) || PInvoke.Kernel32.AllocConsole());
        var hostBuilder = CreateHostBuilder(options, args);
        using var host = hostBuilder.Build();
        host.Start();
        var app = host.Services.GetRequiredService<App>();
        app.InitializeComponent();
        var exitCode = app.Run();
        if (unattendedWithConsole)
        {
            SendKeys.SendWait("{ENTER}");
        }
        return exitCode;
    }

    static IHostBuilder CreateHostBuilder(InstallerOptions options, string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureServices(s => ConfigureServices(s, options))
            .UseConsoleLifetime(x => x.SuppressStatusMessages = true)
            .UseSerilog((_, config) =>
            {
                config.WriteTo.Console(theme: Serilog.Sinks.SystemConsole.Themes.AnsiConsoleTheme.Code, applyThemeToRedirectedOutput: true);
                config.WriteTo.Conditional(e => e.Level <= Serilog.Events.LogEventLevel.Warning, x => x.EventLog(Product.InstallerDisplayName, manageEventSource: true));
            });

    static void ConfigureServices(IServiceCollection services, InstallerOptions options)
    {
        services.AddSingleton(Options.Create(options));
        services.AddSingleton<App>();
        services.AddSingleton<IApplicationContext>(sp => sp.GetRequiredService<App>());
        services.AddSingleton<MainWindow>();
        services.AddSingleton<MainViewModel>();
        services.AddSingleton<IUserInteraction>(sp => sp.GetRequiredService<MainViewModel>());
        services.AddSingleton<IWslService, WslService>();
        services.AddSingleton<IProcessExecutor, ProcessExecutor>();
        services.AddSingleton<IConfigurationManifest>(sp => new PackedConfigurationManifest(App.ConfigurationManifestUri, sp));
        services.AddTransient<IFileSystem, FileSystem>();
        services.AddSingleton<IInstallationRunner, InstallationRunner>();
    }

}
