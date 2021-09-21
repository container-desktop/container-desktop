using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace ContainerDesktop
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainWindow : Window
    {
        private bool _applicationQuit;

        public MainWindow()
        {
            InitializeComponent();
            Title = "Container Desktop";
        }


        public void QuitApplication()
        {
            _applicationQuit = true;
            Close();
        }

        private void WindowClosed(object sender, WindowEventArgs args)
        {
            if (!_applicationQuit)
            {
                var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
                PInvoke.User32.ShowWindow(hwnd, PInvoke.User32.WindowShowStyle.SW_HIDE);
                args.Handled = true;
            }
        }
    }
}
