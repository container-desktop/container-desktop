using ContainerDesktop.Common;
using ContainerDesktop.Common.Services;
using ContainerDesktop.Common.UI;
using ContainerDesktop.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using System;
using System.IO.Abstractions;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace ContainerDesktop
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    public partial class App : Application
    {
        private readonly SystemTrayIcon _systemTrayIcon;
        private MainWindow _window;

        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
            InitializeComponent();
            var configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", true)
                .AddUserSecrets<App>()
                .AddEnvironmentVariables("CONTAINERDESKTOP_")
                .Build();
            var services = new ServiceCollection();
            services.AddSingleton<IConfiguration>(configuration);
            ConfigureServices(services);
            ServiceProvider = services.BuildServiceProvider();
            Environment.SetEnvironmentVariable("INSTALLDIR", AppContext.BaseDirectory.TrimEnd('\\'), EnvironmentVariableTarget.Process);
            var contextMenuBuilder = new ContextMenuBuilder()
                .AddMenuItem("Quit Container Desktop", () =>
                {
                    _systemTrayIcon.Dispose();
                    _window.QuitApplication();
                });
            _systemTrayIcon = new SystemTrayIcon("app.ico", contextMenuBuilder);
            _systemTrayIcon.Activate += (s, e) => _window.Activate();
        }

        public IServiceProvider ServiceProvider { get; private set; }

        /// <summary>
        /// Invoked when the application is launched normally by the end user.  Other entry points
        /// will be used such as when the application is launched to open a specific file.
        /// </summary>
        /// <param name="args">Details about the launch request and process.</param>
        protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            _window = ServiceProvider.GetRequiredService<MainWindow>();
            _window.SetIcon(_systemTrayIcon.IconHandle);
            try
            {
                VerifyConfiguration();
                var bootstrapService = ServiceProvider.GetRequiredService<IBootstrapService>();
                bootstrapService.Bootstrap();
                _systemTrayIcon.Show();
            }
            catch
            {
                _window.QuitApplication();
            }
        }

        private void VerifyConfiguration()
        {
            var dsc = ServiceProvider.GetRequiredService<IDesiredStateConfigurationService>();
            dsc.Apply();
        }

        private void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<MainWindow>();
            //services.AddTransient<MainViewModel>();
            services.AddSingleton<IWslService, WslService>();
            services.AddSingleton<IProcessExecutor, ProcessExecutor>();
            services.AddLogging(builder => builder.AddDebug());
            services.AddSingleton<IDesiredStateConfigurationService, DesiredStateConfigurationService>();
            services.AddSingleton<IBootstrapService, BootstrapService>();
            services.AddTransient<IFileSystem, FileSystem>();
            //services.AddSingleton<InstallerManifest>();
        }
    }
}
