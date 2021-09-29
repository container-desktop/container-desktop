namespace ContainerDesktop;

using ContainerDesktop.ViewModels;
using System.ComponentModel;
using System.Windows;


/// <summary>
/// An empty window that can be used on its own or navigated to within a Frame.
/// </summary>
public partial class MainWindow : Window
{
    private bool _applicationQuit;

    public MainWindow(MainViewModel mainViewModel)
    {
        InitializeComponent();
        DataContext = mainViewModel;
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
            Hide();
            e.Cancel = true;
        }
        else
        {
            base.OnClosing(e);
        }
    }

    private void NavigationViewLoaded(object sender, RoutedEventArgs e)
    {
        
    }

    private void NavigationViewSelectionChanged(ModernWpf.Controls.NavigationView sender, ModernWpf.Controls.NavigationViewSelectionChangedEventArgs args)
    {
        if(args.IsSettingsSelected)
        {
            contentFrame.Navigate(typeof(Settings), null, args.RecommendedNavigationTransitionInfo);
        }
    }
}
