namespace ContainerDesktop;

using System.ComponentModel;
using System.Windows;


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
