using ContainerDesktop.Common;
using ContainerDesktop.Common.Services;
using ContainerDesktop.Common.UI;
using ContainerDesktop.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.IO.Abstractions;
using System.Windows;

namespace ContainerDesktop
{
    public partial class App : Application
    {
        private readonly SystemTrayIcon _systemTrayIcon;
        private MainWindow _window;
        private readonly IServiceScope _rootScope;
        private readonly IServiceProvider _rootServiceProvider;
        
        public App()
        {
            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
            var configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", true)
                .AddUserSecrets<App>()
                .AddEnvironmentVariables("CONTAINERDESKTOP_")
                .Build();
            var services = new ServiceCollection();
            services.AddSingleton<IConfiguration>(configuration);
            ConfigureServices(services);
            _rootServiceProvider = services.BuildServiceProvider();
            _rootScope = _rootServiceProvider.CreateScope();
            ServiceProvider = _rootScope.ServiceProvider;
            Environment.SetEnvironmentVariable("INSTALLDIR", AppContext.BaseDirectory.TrimEnd('\\'), EnvironmentVariableTarget.Process);
            var contextMenuBuilder = new ContextMenuBuilder()
                .AddMenuItem("Quit Container Desktop", () =>
                {
                    QuitApplication();
                });
            _systemTrayIcon = new SystemTrayIcon("app.ico", contextMenuBuilder);
            _systemTrayIcon.Activate += (s, e) => _window.Show();
        }

        public IServiceProvider ServiceProvider { get; private set; }

        protected override void OnStartup(StartupEventArgs e)
        {
            _window = ServiceProvider.GetRequiredService<MainWindow>();
            
            try
            {
                var bootstrapService = ServiceProvider.GetRequiredService<IBootstrapService>();
                bootstrapService.Bootstrap();
                _systemTrayIcon.Show();
            }
            catch
            {
                QuitApplication();
            }
        }

        

        private void QuitApplication()
        {
            _rootScope.Dispose();
            (_rootServiceProvider as IDisposable)?.Dispose();
            _systemTrayIcon.Dispose();
            _window.QuitApplication();
        }

        private void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<MainWindow>();
            //services.AddTransient<MainViewModel>();
            services.AddSingleton<IWslService, WslService>();
            services.AddSingleton<IProcessExecutor, ProcessExecutor>();
            services.AddLogging(builder => builder.AddDebug());
            services.AddSingleton<IBootstrapService, BootstrapService>();
            services.AddTransient<IFileSystem, FileSystem>();
            //services.AddSingleton<InstallerManifest>();
        }
    }
}
