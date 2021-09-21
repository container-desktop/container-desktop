using Microsoft.UI.Xaml;
using System;

namespace ContainerDesktop
{
    static class WindowExtensions
    {
        public static void SetIcon(this Window window, IntPtr iconHandle)
        {
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
            PInvoke.User32.SendMessage(hwnd, PInvoke.User32.WindowMessage.WM_SETICON, IntPtr.Zero, iconHandle);
        }
    }
}
