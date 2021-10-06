using Microsoft.Win32;

namespace ContainerDesktop.DesiredStateConfiguration;

public class GetInstalledVersion : ResourceBase
{
    private const string ProductUninstallRegistryKeyTemplate = "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Uninstall\\{0}";

    public string ProductName { get; set; }

    public override void Set(ConfigurationContext context)
    {
        var keyName = string.Format(ProductUninstallRegistryKeyTemplate, Environment.ExpandEnvironmentVariables(ProductName));
        using var key = Registry.LocalMachine.OpenSubKey(keyName);
        if(key != null)
        {
            context.InstalledVersion = (string)key.GetValue("Version");
        }
    }

    public override void Unset(ConfigurationContext context)
    {
        Set(context);
    }

    public override bool Test(ConfigurationContext context)
    {
        return false;
    }
}
