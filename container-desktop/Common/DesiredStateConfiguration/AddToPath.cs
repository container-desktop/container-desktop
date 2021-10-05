namespace ContainerDesktop.Common.DesiredStateConfiguration;

using System.Runtime.InteropServices;
using static PInvoke.User32;

public class AddToPath : ResourceBase
{
    public string Path { get; set; }

    private string ExpandedPath => Environment.ExpandEnvironmentVariables(Path);

    public override void Set(ConfigurationContext context)
    {
        var path = GetPath();
        var parts = path.Split(';').ToList();
        var index = parts.FindIndex(s => s.Equals(ExpandedPath, StringComparison.OrdinalIgnoreCase));
        if (index < 0)
        {
            path = $"{path};{ExpandedPath}";
            SetPath(path);
        }
    }

    public override void Unset(ConfigurationContext context)
    {
        var path = GetPath();
        var parts = path.Split(';').ToList();
        var index = parts.FindIndex(s => s.Equals(ExpandedPath, StringComparison.OrdinalIgnoreCase));
        if(index >= 0)
        {
            parts.RemoveAt(index);
            path = string.Join(';', parts);
            SetPath(path);
        }
    }

    public override bool Test(ConfigurationContext context)
    {
        var path = GetPath();
        var parts = path.Split(';');
        return parts.Contains(ExpandedPath, StringComparer.OrdinalIgnoreCase);
    }

    private string GetPath()
    {
        return Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.User);
    }

    private void SetPath(string path)
    {
        Environment.SetEnvironmentVariable("PATH", path, EnvironmentVariableTarget.User);
        var lParam = Marshal.StringToHGlobalUni("Environment");
        SendMessageTimeout(
            HWND_BROADCAST,
            WindowMessage.WM_SETTINGCHANGE,
            IntPtr.Zero,
            lParam,
            SendMessageTimeoutFlags.SMTO_ABORTIFHUNG,
            2000,
            out var _);
        Marshal.FreeHGlobal(lParam);
    }
}
