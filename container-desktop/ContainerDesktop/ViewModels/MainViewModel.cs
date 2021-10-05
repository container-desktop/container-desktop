namespace ContainerDesktop.ViewModels;

using ContainerDesktop.Common;
using ContainerDesktop.Common.Input;
using ContainerDesktop.Common.Services;
using ContainerDesktop.Common.UI;
using ContainerDesktop.Services;
using System.Reactive.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;

public class MainViewModel : ViewModelBase
{
    private readonly IApplicationContext _applicationContext;
    private readonly IContainerEngine _containerEngine;
    private readonly IWslService _wslService;
    private readonly IConfigurationService _configurationService;
    private readonly IProcessExecutor _processExecutor;
    private bool _showTrayIcon;
    private bool _isStarted;
    private BitmapImage _trayIcon = _icon; // "/ContainerDesktop;component/app.ico";

    private static BitmapImage _icon = new BitmapImage(new Uri("pack://application:,,,/app.ico"));
    private static BitmapImage _runIcon = new BitmapImage(new Uri("pack://application:,,,/app_run.ico"));
    private static BitmapImage _stopIcon = new BitmapImage(new Uri("pack://application:,,,/app_stop.ico"));

    public MainViewModel(
        IApplicationContext applicationContext, 
        IContainerEngine containerEngine, 
        IWslService wslService, 
        IConfigurationService configurationService,
        IProcessExecutor processExecutor)
    {
        _applicationContext = applicationContext;
        _containerEngine = containerEngine;
        _containerEngine.RunningStateChanged += RunnningStateChanged;
        _wslService = wslService;
        _configurationService = configurationService;
        _processExecutor = processExecutor;
        OpenCommand = new DelegateCommand(Open);
        QuitCommand = new DelegateCommand(Quit);
        StartCommand = new DelegateCommand(Start, () => _containerEngine.RunningState == RunningState.Stopped);
        StopCommand = new DelegateCommand(Stop, () => _containerEngine.RunningState == RunningState.Started);
        RestartCommand = new DelegateCommand(Restart, () => _containerEngine.RunningState == RunningState.Started);
        CheckWslDistroCommand = new DelegateCommand(ToggleWslDistro);
        OpenDocumentationCommand = new DelegateCommand(OpenDocumentation);
    }

    public bool ShowTrayIcon
    {
        get => _showTrayIcon;
        set => SetValueAndNotify(ref _showTrayIcon, value);
    }

    public BitmapImage TrayIcon
    {
        get => _trayIcon;
        set => SetValueAndNotify(ref _trayIcon, value);
    }

    public bool IsStarted
    {
        get => _isStarted;
        set
        {
            if (SetValueAndNotify(ref _isStarted, value))
            {
                NotifyPropertyChanged(nameof(IsStopped));
            }
        }
    }

    public bool IsStopped => !IsStarted;

    public DelegateCommand OpenCommand { get; }

    public ICommand QuitCommand { get; }

    public DelegateCommand StartCommand { get; }

    public DelegateCommand StopCommand { get; }

    public DelegateCommand RestartCommand { get; }

    public DelegateCommand CheckWslDistroCommand { get; }

    public DelegateCommand OpenDocumentationCommand { get; }

    public IEnumerable<WslDistributionItem> WslDistributions =>
        _wslService.GetDistros()
            .Where(x => !_configurationService.Configuration.HiddenDistributions.Contains(x))
            .Select(x => new WslDistributionItem { Name = x, Enabled = _configurationService.Configuration.EnabledDistributions.Contains(x)});
    
    private void Open(object parameter)
    {
        // TODO: uncomment when window has actually something to show.
        //_applicationContext.ShowMainWindow();
    }

    private void Quit(object parameter)
    {
        _applicationContext.QuitApplication();
    }

    private void Stop(object parameter)
    {
        Task.Run(() => SafeExecute("stop", _containerEngine.Stop));
    }

    private void Start(object parameter)
    {
        Task.Run(() => SafeExecute("start", _containerEngine.Start));
    }

    private void Restart(object parameter)
    {
        Task.Run(() => SafeExecute("restart", _containerEngine.Restart));
    }

    private void RunnningStateChanged(object sender, EventArgs e)
    {
        _applicationContext.Dispatcher.Invoke(() =>
        {
            IsStarted = _containerEngine.RunningState == RunningState.Started;
            StartCommand.RaiseCanExecuteChanged();
            StopCommand.RaiseCanExecuteChanged();
            RestartCommand.RaiseCanExecuteChanged();
            TrayIcon = _containerEngine.RunningState switch
            {
                RunningState.Started => _runIcon,
                RunningState.Stopped => _stopIcon,
                _ => _icon
            };
        });
    }

    private void ToggleWslDistro(object parameter)
    {
        Task.Run(() =>
        {
            if (parameter is WslDistributionItem distro)
            {
                SafeExecute($"{(distro.Enabled ? "enable" : "disable")} distribution", () =>
                {
                    if (_containerEngine.RunningState == RunningState.Started)
                    {
                        _containerEngine.EnableDistro(distro.Name, distro.Enabled);
                    }
                    if (distro.Enabled)
                    {
                        if (!_configurationService.Configuration.EnabledDistributions.Contains(distro.Name))
                        {
                            _configurationService.Configuration.EnabledDistributions.Add(distro.Name);
                        }
                    }
                    else
                    {
                        _configurationService.Configuration.EnabledDistributions.Remove(distro.Name);
                    }
                    _configurationService.Save();
                });
            }
        });
    }

    private void OpenDocumentation(object parameter)
    {
        _processExecutor.Start(Product.WebSiteUrl, null, useShellExecute: true);
    }

    private void SafeExecute(string caption, Action action)
    {
        try
        {
            action();
        }
        catch (Exception ex)
        {
            _applicationContext.Dispatcher.Invoke(() =>
                MessageBox.Show(ex.Message, $"Failed to {caption}", MessageBoxButton.OK));
        }
    }
}

