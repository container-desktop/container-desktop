namespace ContainerDesktop.ViewModels;

using ContainerDesktop.Common;
using ContainerDesktop.Common.Input;
using ContainerDesktop.Common.UI;
using ContainerDesktop.Services;
using System.Windows.Input;

public class MainViewModel : ViewModelBase
{
    private readonly IApplicationContext _applicationContext;
    private readonly IContainerEngine _containerEngine;
    private bool _showTrayIcon;
    private bool _isStarted;

    public MainViewModel(IApplicationContext applicationContext, IContainerEngine containerEngine)
    {
        _applicationContext = applicationContext;
        _containerEngine = containerEngine;
        _containerEngine.RunningStateChanged += RunnningStateChanged;
        OpenCommand = new DelegateCommand(Open);
        QuitCommand = new DelegateCommand(Quit);
        StartCommand = new DelegateCommand(Start, () => _containerEngine.RunningState == RunningState.Stopped);
        StopCommand = new DelegateCommand(Stop, () => _containerEngine.RunningState == RunningState.Started);
        RestartCommand = new DelegateCommand(Restart, () => _containerEngine.RunningState == RunningState.Started);
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
        _containerEngine.Stop();
    }

    private void Start(object parameter)
    {
        _containerEngine.Start();
    }

    private void Restart(object parameter)
    {
        _containerEngine.Restart();
    }

    private void RunnningStateChanged(object sender, EventArgs e)
    {
        IsStarted = _containerEngine.RunningState == RunningState.Started;
        StartCommand.RaiseCanExecuteChanged();
        StopCommand.RaiseCanExecuteChanged();
        RestartCommand.RaiseCanExecuteChanged();
    }
}

