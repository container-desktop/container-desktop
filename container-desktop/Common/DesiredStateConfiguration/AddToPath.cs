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
            var parts = path.Split(';');
            if (!parts.Contains(expandedPath, StringComparer.OrdinalIgnoreCase))
            {
                path = $"{path};{expandedPath}";
                SetPath(path);
            }
        }

        public override bool Test(ConfigurationContext context)
        {
            var expandedPath = Environment.ExpandEnvironmentVariables(Path);
            var path = GetPath();
            var parts = path.Split(';');
            return parts.Contains(expandedPath, StringComparer.OrdinalIgnoreCase);
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
