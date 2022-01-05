namespace ContainerDesktop.Common;
public class ProductInformation : IProductInformation
{
    public ProductInformation()
    {
        InstallDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), Name);
        AppPath = Path.Combine(InstallDir, $"{Name}.exe");
        ProxyPath = Path.Combine(InstallDir, "Resources", $"container-desktop-proxy-windows-amd64.exe");
        ContainerDesktopAppDataDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), Name);
    }

    public string Name { get; } = "ContainerDesktop";
    public string DisplayName { get; } = "Container Desktop";
    public string InstallerDisplayName { get; } = "Container Desktop Installer";
    public string Version { get; } = GetVersion();
    public string InstallDir { get; } 
    public string AppPath { get; }
    public string ProxyPath { get; } 
    public string ContainerDesktopDistroName { get; } = "container-desktop";
    public string ContainerDesktopDataDistroName { get; } = "container-desktop-data";
    public string ContainerDesktopAppDataDir { get; } 
    public string WebSiteUrl { get; } = "https://container-desktop.io";
    
    private static string GetVersion()
    {
        var version = ThisAssembly.AssemblyInformationalVersion;
        var index = version.IndexOf('+');
        if (index >= 0)
        {
            return version[0..index];
        }
        return version;
    }
}
