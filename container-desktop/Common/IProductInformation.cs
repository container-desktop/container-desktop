namespace ContainerDesktop.Common;

public interface IProductInformation
{
    string Name { get; }
    string DisplayName { get; }
    string InstallerDisplayName { get; }
    string InstallDir { get; }
    string AppPath { get; }
    string ProxyPath { get; }
    string PortForwarderPath { get; }
    string ContainerDesktopDistroName { get; }
    string ContainerDesktopDataDistroName { get; }
    string ContainerDesktopAppDataDir { get; }
    string WebSiteUrl { get; }
    string Version { get; }
}

