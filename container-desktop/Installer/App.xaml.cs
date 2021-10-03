namespace ContainerDesktop.Installer;

using ContainerDesktop.Common;
using ContainerDesktop.Common.DesiredStateConfiguration;
using ContainerDesktop.Common.Services;
using ContainerDesktop.Installer.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics;
using System.IO.Abstractions;
using System.Windows;


/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : ApplicationWithContext
{
    public static readonly Uri ConfigurationManifestUri = new Uri($"pack://application:,,,/{typeof(App).Assembly.GetName().Name};component/Resources/configuration-manifest.json");
    
    public App()
    {
        // Not used. But is called by the generated Main, which is also not used.
    }

    public App(IServiceProvider serviceProvider)
    {
        ServiceProvider = serviceProvider;
    }

    public IServiceProvider ServiceProvider { get; private set; }

    private void AppStartup(object sender, StartupEventArgs e)
    {
        SetEnvironmentVariables();
        var runner = ServiceProvider.GetRequiredService<IInstallationRunner>();
        if (runner.InstallationMode == InstallationMode.Uninstall && Path.GetDirectoryName(GetInstallerExePath()).Equals(Product.InstallDir, StringComparison.OrdinalIgnoreCase))
        {
            RestartInTempLocation();
            Shutdown();
        }
        else
        {
            MainWindow = ServiceProvider.GetRequiredService<MainWindow>();
            if (!runner.Options.Unattended)
            {
                MainWindow.Show();
            }
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

    private void AppExit(object sender, ExitEventArgs e)
    {
        e.ApplicationExitCode = ExitCode;
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
        Environment.SetEnvironmentVariable("PROXY_PATH", Product.ProxyPath, EnvironmentVariableTarget.Process);
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
