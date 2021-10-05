namespace ContainerDesktop.Common;

using System.Reflection;

public static class Product
{
    public static string Name { get; } = "ContainerDesktop";
    public static string DisplayName { get; } = "Container Desktop";
    public static string InstallerDisplayName { get; } = "Container Desktop Installer";
    public static string Version { get; } = GetVersion();
    public static string InstallDir { get; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), Name);
    public static string AppPath { get; } = Path.Combine(InstallDir, $"{Name}.exe");
    public static string ProxyPath { get; } = Path.Combine(InstallDir, "Resources", $"container-desktop-proxy-windows-amd64.exe");
    public static string ContainerDesktopDistroName { get; } = "container-desktop";
    public static string ContainerDesktopDataDistroName { get; } = "container-desktop-data";
    public static string ContainerDesktopAppDataDir { get; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), Name);
    public static string WebSiteUrl { get; } = "https://container-desktop.io";
    private static string GetVersion()
    {
        return ThisAssembly.AssemblyInformationalVersion;
    }
}
