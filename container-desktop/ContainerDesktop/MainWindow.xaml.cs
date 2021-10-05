namespace ContainerDesktop;

using ContainerDesktop.ViewModels;
using System.ComponentModel;
using System.Windows;
using System.Windows.Interop;


/// <summary>
/// An empty window that can be used on its own or navigated to within a Frame.
/// </summary>
public partial class MainWindow : Window
{
    private const int WM_APP_QUIT = (int)PInvoke.User32.WindowMessage.WM_APP + 1;

    private bool _applicationQuit;
    private readonly ILogger<MainWindow> _logger;

    public MainWindow(MainViewModel mainViewModel, ILogger<MainWindow> logger)
    {
        _logger = logger;
        InitializeComponent();
        DataContext = mainViewModel;
        var helper = new WindowInteropHelper(this);
        var handle = helper.EnsureHandle();
        logger.LogInformation($"MainWindow handle: {handle:X}");
        var source = HwndSource.FromHwnd(handle);
        source.AddHook(HwndProcHook);
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

    private IntPtr HwndProcHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if(msg == WM_APP_QUIT)
        {
            _logger.LogInformation("Received WM_APP_QUIT, quiting application");
            handled = true;
            QuitApplication();
        }
        return IntPtr.Zero;
    }
}
