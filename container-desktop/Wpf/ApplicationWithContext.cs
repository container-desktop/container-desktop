namespace ContainerDesktop.UI.Wpf;

using ContainerDesktop.Common;
using System;
using System.Text;
using System.Windows;

public class ApplicationWithContext : Application, IApplicationContext
{
    public int ExitCode { get; set; }
    public string LastErrorMessage { get; set; }

    public ApplicationWithContext()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }

    public virtual void QuitApplication()
    {
        MainWindow.Close();
    }

    public virtual void ShowMainWindow()
    {
        MainWindow.Show();
    }

    public void InvokeOnDispatcher(Action action)
    {
        Dispatcher.Invoke(action);
    }
}
