using Microsoft.Win32;

namespace ContainerDesktop.Common;

public static class AutoStartHelper
{
    private const string RunRegistryKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";

    public static void EnableAutoStart(string executablePath)
    {
        if (executablePath == null)
        {
            throw new ArgumentNullException(nameof(executablePath));
        }
        using var key = Registry.CurrentUser.CreateSubKey(RunRegistryKey);
        var name = Path.GetFileNameWithoutExtension(executablePath);
        key.SetValue(name, executablePath);
    }

    public static void DisableAutoStart(string executablePath)
    {
        if (executablePath == null)
        {
            throw new ArgumentNullException(nameof(executablePath));
        }
        using var key = Registry.CurrentUser.CreateSubKey(RunRegistryKey);
        var name = Path.GetFileNameWithoutExtension(executablePath);
        key.DeleteValue(name);
    }

    public static void SetAutoStart(bool enable, string executablePath)
    {
        if(enable)
        {
            EnableAutoStart(executablePath);
        }
        else
        {
            DisableAutoStart(executablePath);
        }
    }

    public static bool IsAutoStartEnabled(string executablePath)
    {
        if (executablePath == null)
        {
            throw new ArgumentNullException(nameof(executablePath));
        }
        using var key = Registry.CurrentUser.CreateSubKey(RunRegistryKey);
        var name = Path.GetFileNameWithoutExtension(executablePath);
        var value = (string)key.GetValue(name);
        return executablePath.Equals(value, StringComparison.OrdinalIgnoreCase);
    }
}
