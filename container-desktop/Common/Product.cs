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

    private static string GetVersion()
    {
        return Assembly.GetEntryAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion;
    }
}
