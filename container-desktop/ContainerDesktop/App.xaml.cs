namespace ContainerDesktop;

using ContainerDesktop.Common;
using ContainerDesktop.Common.Services;
using ContainerDesktop.Common.UI;
using ContainerDesktop.Services;
using ContainerDesktop.ViewModels;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.IO.Abstractions;
using System.Windows;


public partial class App : ApplicationWithContext
{
    public App()
    {
        ServiceProvider = SetupServiceProvider();
        Logger = ServiceProvider.GetRequiredService<ILogger<App>>();
    }

    public IServiceProvider ServiceProvider { get; private set; }

    public ILogger<App> Logger { get; }

    public MainViewModel MainViewModel { get; set; }

    protected override void OnStartup(StartupEventArgs e)
    {
        try
        {
            MainWindow = ServiceProvider.GetRequiredService<MainWindow>();
            MainViewModel = ServiceProvider.GetRequiredService<MainViewModel>();
            var engine = ServiceProvider.GetRequiredService<IContainerEngine>();
            engine.Start();
            MainViewModel.ShowTrayIcon = true;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, ex.Message);
            MessageBox.Show($"{Product.DisplayName} failed to startup, please view the event log for errors.", "Failed to start", MessageBoxButton.OK);
            QuitApplication();
        }
    }

    public override void QuitApplication()
    {
        (ServiceProvider as IDisposable)?.Dispose();
        Shutdown(ExitCode);
    }

    private IServiceProvider SetupServiceProvider()
    {
        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", true)
            .AddUserSecrets<App>()
            .AddEnvironmentVariables("CONTAINERDESKTOP_")
            .Build();
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        ConfigureServices(services);
        return services.BuildServiceProvider();
    }

    private void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<IApplicationContext>(this);
        services.AddSingleton<MainWindow>();
        services.AddSingleton<MainViewModel>();
        services.AddSingleton<IWslService, WslService>();
        services.AddSingleton<IProcessExecutor, ProcessExecutor>();
        services.AddLogging(builder =>
            builder
                .AddDebug()
                .AddEventLog(settings =>
                {
                    settings.SourceName = Product.DisplayName;
                })
            );
        services.AddSingleton<IContainerEngine, DefaultContainerEngine>();
        services.AddTransient<IFileSystem, FileSystem>();
    }
}
