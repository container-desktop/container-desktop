using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace ContainerDesktop
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public partial class MainWindow : Window
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

        protected override void OnClosing(CancelEventArgs e)
        {
            if (!_applicationQuit)
            {
                this.Hide();
                e.Cancel = true;
            }
            else
            {
                base.OnClosing(e);
            }
        }
    }
}
