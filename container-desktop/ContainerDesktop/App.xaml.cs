namespace ContainerDesktop;

using ContainerDesktop.Common;
using ContainerDesktop.Services;
using ContainerDesktop.UI.Wpf;
using ContainerDesktop.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;

#pragma warning disable CA2254

public partial class App : ApplicationWithContext
{
    public App()
    {
        // Not used. But is called by the generated Main, which is also not used.
    }

    public App(IServiceProvider serviceProvider, ILogger<App> logger)
    {
        ServiceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public IServiceProvider ServiceProvider { get; }

    public ILogger<App> Logger { get; }

    public MainViewModel MainViewModel { get; private set; }

    public IContainerEngine ContainerEngine { get; private set; }

    private void AppStartup(object sender, StartupEventArgs e)
    {
        MainWindow = ServiceProvider.GetRequiredService<MainWindow>();
        MainViewModel = ServiceProvider.GetRequiredService<MainViewModel>();
        ContainerEngine = ServiceProvider.GetRequiredService<IContainerEngine>();
        var productInfo = ServiceProvider.GetRequiredService<IProductInformation>();
        new WindowInteropHelper(MainWindow).EnsureHandle();
        MainViewModel.ShowTrayIcon = true;
        Task.Run(() => ContainerEngine.Start()).ToObservable().Subscribe(_ => { }, ex =>
        {
            Logger.LogError(ex, ex.Message);
            Dispatcher.Invoke(() =>
            {
                MessageBox.Show(MainWindow, $"{productInfo.DisplayName} failed to startup, please view the event log for errors.\r\n\r\nMessage:\r\n{ex.Message}", "Failed to start", MessageBoxButton.OK);
                QuitApplication();
            });
        });
    }

    public override void QuitApplication()
    {
        (MainWindow as MainWindow)?.QuitApplication();
    }

    public override void ShowSettings()
    {
        var mainWindow = (MainWindow)MainWindow;
        mainWindow.ShowSettings();
    }
}

#pragma warning restore CA2254
