using Microsoft.Win32;

namespace ContainerDesktop.Common.DesiredStateConfiguration;

public class AddToAutoStart : ResourceBase
{
    private const string RunRegistryKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";

    public string ExePath { get; set; }

    private string ExpandedExePath => Environment.ExpandEnvironmentVariables(ExePath);

    public override void Set(ConfigurationContext context)
    {
        using var key = Registry.CurrentUser.CreateSubKey(RunRegistryKey);
        var name = Path.GetFileNameWithoutExtension(ExpandedExePath);
        if (context.Uninstall)
        {
            key.DeleteValue(name);
        }
        else
        {
            key.SetValue(name, ExpandedExePath);
        }
    }

    public override bool Test(ConfigurationContext context)
    {
        using var key = Registry.CurrentUser.CreateSubKey(RunRegistryKey);
        var name = Path.GetFileNameWithoutExtension(ExpandedExePath);
        var value = (string) key.GetValue(name);
        var exists = ExpandedExePath.Equals(value, StringComparison.OrdinalIgnoreCase);
        if(context.Uninstall)
        {
            exists = !exists;
        }
        return exists;
    }
}
