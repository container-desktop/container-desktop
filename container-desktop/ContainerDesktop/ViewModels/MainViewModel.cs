namespace ContainerDesktop.ViewModels;

using ContainerDesktop.Common;
using ContainerDesktop.Common.Input;
using ContainerDesktop.Common.UI;
using System.Windows.Input;

public class MainViewModel : ViewModelBase
{
    private readonly IApplicationContext _applicationContext;
    private bool _showTrayIcon;

    public MainViewModel(IApplicationContext applicationContext)
    {
        _applicationContext = applicationContext;
        OpenCommand = new DelegateCommand(Open);
        QuitCommand = new DelegateCommand(Quit);
    }

    public bool ShowTrayIcon
    {
        get => _showTrayIcon;
        set => SetValueAndNotify(ref _showTrayIcon, value);
    }

    public ICommand OpenCommand { get; }

    public ICommand QuitCommand { get; }

    private void Open(object parameter)
    {
        // TODO: uncomment when window has actually something to show.
        //_applicationContext.ShowMainWindow();
    }

    private void Quit(object parameter)
    {
        _applicationContext.QuitApplication();
    }
}

