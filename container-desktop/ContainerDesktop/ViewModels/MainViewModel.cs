namespace ContainerDesktop.ViewModels;

using ContainerDesktop.Common;
using ContainerDesktop.Common.Input;
using ContainerDesktop.Common.Services;
using ContainerDesktop.Common.UI;
using ContainerDesktop.Services;
using System.Windows;
using System.Windows.Input;

public class MainViewModel : ViewModelBase
{
    private readonly IApplicationContext _applicationContext;
    private readonly IContainerEngine _containerEngine;
    private readonly IWslService _wslService;
    private readonly IConfigurationService _configurationService;
    private bool _showTrayIcon;
    private bool _isStarted;

    public MainViewModel(IApplicationContext applicationContext, IContainerEngine containerEngine, IWslService wslService, IConfigurationService configurationService)
    {
        _applicationContext = applicationContext;
        _containerEngine = containerEngine;
        _containerEngine.RunningStateChanged += RunnningStateChanged;
        _wslService = wslService;
        _configurationService = configurationService;
        OpenCommand = new DelegateCommand(Open);
        QuitCommand = new DelegateCommand(Quit);
        StartCommand = new DelegateCommand(Start, () => _containerEngine.RunningState == RunningState.Stopped);
        StopCommand = new DelegateCommand(Stop, () => _containerEngine.RunningState == RunningState.Started);
        RestartCommand = new DelegateCommand(Restart, () => _containerEngine.RunningState == RunningState.Started);
        CheckWslDistroCommand = new DelegateCommand(ToggleWslDistro);
    }

    public bool ShowTrayIcon
    {
        get => _showTrayIcon;
        set => SetValueAndNotify(ref _showTrayIcon, value);
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
        SafeExecute("stop", _containerEngine.Stop);
    }

    private void Start(object parameter)
    {
        SafeExecute("start", _containerEngine.Start);
    }

    private void Restart(object parameter)
    {
        SafeExecute("restart", _containerEngine.Restart);
    }

    private void RunnningStateChanged(object sender, EventArgs e)
    {
        IsStarted = _containerEngine.RunningState == RunningState.Started;
        StartCommand.RaiseCanExecuteChanged();
        StopCommand.RaiseCanExecuteChanged();
        RestartCommand.RaiseCanExecuteChanged();
    }

    private void ToggleWslDistro(object parameter)
    {
        if(parameter is WslDistributionItem distro)
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
    }

    private void SafeExecute(string caption, Action action)
    {
        try
        {
            action();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, $"Failed to {caption}", MessageBoxButton.OK);
        }
    }
}

