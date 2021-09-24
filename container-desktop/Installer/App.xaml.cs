using ContainerDesktop.Common;
using ContainerDesktop.Common.Cli;
using ContainerDesktop.Common.DesiredStateConfiguration;
using ContainerDesktop.Common.Services;
using ContainerDesktop.Installer.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.IO;
using System.IO.Abstractions;
using System.Windows;

namespace ContainerDesktop.Installer
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application, IApplicationContext
    {
        private static readonly Uri ConfigurationManifestUri = new Uri($"pack://application:,,,/{typeof(App).Assembly.GetName().Name};component/Resources/configuration-manifest.json");
        private InstallationRunner _runner;

        public App()
        {
            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
        }

        public IServiceProvider ServiceProvider => _runner.ServiceProvider;

        public int ExitCode { get; set; }

        public void QuitApplication()
        {
            MainWindow.Close();
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            Setup(e.Args);
            if (_runner.InstallationMode == InstallationMode.Uninstall && Path.GetDirectoryName(GetInstallerExePath()).Equals(Product.InstallDir, StringComparison.OrdinalIgnoreCase))
            {
                RestartInTempLocation();
                Shutdown();
            }
            else
            {
                MainWindow = ServiceProvider.GetRequiredService<MainWindow>();
                MainWindow.Show();
            }
        }

        private void RestartInTempLocation()
        {
            var fileSystem = ServiceProvider.GetRequiredService<IFileSystem>();
            var tmpFileDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            fileSystem.Directory.CreateDirectory(tmpFileDir);
            var source = GetInstallerExePath();
            var target = Path.Combine(tmpFileDir, Path.GetFileName(source));
            fileSystem.File.Copy(source, target);
            Process.Start(target, Environment.GetCommandLineArgs()[1..]);
        }

        protected override void OnExit(ExitEventArgs e)
        {
            e.ApplicationExitCode = ExitCode;
            _runner.Dispose();
        }

        private void Setup(string[] args)
        {
            var services = new ServiceCollection();
            ConfigureServices(services);
            SetEnvironmentVariables();
            _runner = new InstallationRunner(services, args);
        }

        private void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<IApplicationContext>(this);
            services.AddSingleton<MainWindow>();
            services.AddSingleton<MainViewModel>();
            services.AddSingleton<IUserInteraction>(sp => sp.GetService<MainViewModel>());
            services.AddSingleton<IWslService, WslService>();
            services.AddSingleton<IProcessExecutor, ProcessExecutor>();
            services.AddLogging(builder =>
                builder.AddDebug()
                    .AddEventLog(settings =>
                    {
                        settings.SourceName = Product.InstallerDisplayName;
                    }));
            services.AddSingleton<IConfigurationManifest>(sp => new PackedConfigurationManifest(ConfigurationManifestUri, sp));
            services.AddTransient<IProcessor<InstallOptions>, InstallProcessor>();
            services.AddTransient<IProcessor<UninstallOptions>, UninstallProcessor>();
            services.AddTransient<IFileSystem, FileSystem>();
        }

        private void SetEnvironmentVariables()
        {
            Environment.SetEnvironmentVariable("INSTALLDIR", Product.InstallDir, EnvironmentVariableTarget.Process);
            Environment.SetEnvironmentVariable("PRODUCT_VERSION", Product.Version, EnvironmentVariableTarget.Process);
            Environment.SetEnvironmentVariable("INSTALLER_SOURCE_PATH", GetInstallerExePath(), EnvironmentVariableTarget.Process);
            Environment.SetEnvironmentVariable("INSTALLER_TARGET_PATH", Path.Combine(Product.InstallDir, Path.GetFileName(GetInstallerExePath())), EnvironmentVariableTarget.Process);
            Environment.SetEnvironmentVariable("APP_PATH", Product.AppPath, EnvironmentVariableTarget.Process);
            Environment.SetEnvironmentVariable("PRODUCT_DISPLAYNAME", Product.DisplayName, EnvironmentVariableTarget.Process);
            Environment.SetEnvironmentVariable("PRODUCT_NAME", Product.Name, EnvironmentVariableTarget.Process);
        }

        private string GetInstallerExePath()
        {
            var path = Environment.GetCommandLineArgs()[0];
            if (Path.GetExtension(path) == ".dll")
            {
                path = Path.ChangeExtension(path, ".exe");
            }
            return path;
        }
    }
}
