using ContainerDesktop.Common.DesiredStateConfiguration;

namespace ContainerDesktop.Installer;

public interface IInstallationRunner
{
    IConfigurationManifest ConfigurationManifest { get; }
    InstallationMode InstallationMode { get; }
    InstallerOptions Options { get; }
    void Run(Action<ConfigurationContext> configure = null);
}
