namespace ContainerDesktop;

using ContainerDesktop.Pages;
using ContainerDesktop.ViewModels;
using Microsoft.Extensions.DependencyInjection;
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
    private readonly MainPage _mainPage;

    public MainWindow(MainViewModel mainViewModel, ILogger<MainWindow> logger, MainPage mainPage)
    {
        _logger = logger;
        _mainPage = mainPage;
        InitializeComponent();
        DataContext = mainViewModel;
        var helper = new WindowInteropHelper(this);
        var handle = helper.EnsureHandle();
        logger.LogInformation("MainWindow handle: {MainWindowHandle}", $"{handle:X}");
        var source = HwndSource.FromHwnd(handle);
        source.AddHook(HwndProcHook);
        mainFrame.Navigate(mainPage);
    }

    public void QuitApplication()
    {
        _applicationQuit = true;
        Close();
    }

    public void ShowSettings()
    {
        Show();
        _mainPage.ShowSettings();
    }

    public void ShowMainPage()
    {
        mainFrame.Navigate(_mainPage);
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
