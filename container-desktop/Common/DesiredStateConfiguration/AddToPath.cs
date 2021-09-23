using System;
using System.Linq;
using System.Runtime.InteropServices;
using static PInvoke.User32;

namespace ContainerDesktop.Common.DesiredStateConfiguration
{
    public class AddToPath : ResourceBase
    {
        public string Path { get; set; }

        public override void Set(ConfigurationContext context)
        {
            var expandedPath = Environment.ExpandEnvironmentVariables(Path);
            var path = GetPath();
            var parts = path.Split(';').ToList();
            var index = parts.FindIndex(s => s.Equals(expandedPath, StringComparison.OrdinalIgnoreCase));
            if (!context.Uninstall && index < 0)
            {
                path = $"{path};{expandedPath}";
                SetPath(path);
            }
            else if(context.Uninstall && index >= 0)
            {
                parts.RemoveAt(index);
                path = string.Join(';', parts);
                SetPath(path);
            }
        }

        public override bool Test(ConfigurationContext context)
        {
            var expandedPath = Environment.ExpandEnvironmentVariables(Path);
            var path = GetPath();
            var parts = path.Split(';');
            var ret = parts.Contains(expandedPath, StringComparer.OrdinalIgnoreCase);
            if(context.Uninstall)
            {
                ret = !ret;
            }
            return ret;
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
                5000,
                out var _);
            Marshal.FreeHGlobal(lParam);
        }
    }
}
