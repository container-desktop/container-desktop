namespace ContainerDesktop.Installer;

public interface IInstallationRunner
{
    InstallationMode InstallationMode { get; }
    InstallerOptions Options { get; }
    Task<int> RunAsync();
}
