namespace ContainerDesktop.Common.DesiredStateConfiguration;

using Microsoft.Win32;

public class RegisterProduct : ResourceBase
{
    private const string ProductUninstallRegistryKeyTemplate = "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Uninstall\\{0}";

    public string ProductName { get; set; }
    public string DisplayIcon { get; set; }
    public string DisplayName { get; set; }
    public string DisplayVersion { get; set; }
    public string Version { get; set; }
    public string InstallLocation { get; set; }
    public bool NoModify { get; set; } = true;
    public bool NoRepair { get; set; } = true;
    public string UninstallString { get; set; }

    public override void Set(ConfigurationContext context)
    {
        var keyName = string.Format(ProductUninstallRegistryKeyTemplate, Expand(ProductName));
        if (context.Uninstall)
        {
            Registry.LocalMachine.DeleteSubKeyTree(keyName);
        }
        else
        {
            using var registryKey = Registry.LocalMachine.CreateSubKey(keyName);
            registryKey.SetValue("DisplayIcon", Expand(DisplayIcon));
            registryKey.SetValue("DisplayName", Expand(DisplayName));
            registryKey.SetValue("DisplayVersion", Expand(DisplayVersion));
            registryKey.SetValue("Version", Expand(Version));
            registryKey.SetValue("InstallLocation", Expand(InstallLocation));
            registryKey.SetValue("NoModify", NoModify ? 1 : 0);
            registryKey.SetValue("NoRepair", NoRepair ? 1 : 0);
            registryKey.SetValue("UninstallString", Expand(UninstallString));
        }
    }

    public override bool Test(ConfigurationContext context)
    {
        var keyName = string.Format(ProductUninstallRegistryKeyTemplate, Expand(ProductName));
        using var key = Registry.LocalMachine.OpenSubKey(keyName);
        if (context.Uninstall)
        {
            return key == null;
        }
        else if (key != null)
        {
            var version = (string)key.GetValue("Version");
            return version == Expand(Version);
        }
        return false;
    }

    private string Expand(string s) => Environment.ExpandEnvironmentVariables(s);
}
