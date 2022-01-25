using ContainerDesktop.Abstractions;
using ContainerDesktop.Common;
using ContainerDesktop.Configuration;
using ContainerDesktop.Pages;
using ContainerDesktop.Processes;
using ContainerDesktop.Services;
using ContainerDesktop.UI.Wpf.Input;
using ContainerDesktop.Wsl;
using NuGet.Versioning;
using System.Diagnostics;
using System.IO.Abstractions;
using System.Net.Http;
using System.Net.Http.Json;
using System.Net.NetworkInformation;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace ContainerDesktop.ViewModels;

#pragma warning disable CA2254

public class MainViewModel : NotifyObject
{
    private static readonly BitmapImage _icon = new(new Uri("pack://application:,,,/app.ico"));
    private static readonly BitmapImage _runIcon = new(new Uri("pack://application:,,,/app_run.ico"));
    private static readonly BitmapImage _stopIcon = new(new Uri("pack://application:,,,/app_stop.ico"));

    private readonly IApplicationContext _applicationContext;
    private readonly IContainerEngine _containerEngine;
    private readonly IWslService _wslService;
    private readonly IConfigurationService _configurationService;
    private readonly IProcessExecutor _processExecutor;
    private readonly ILogger<MainViewModel> _logger;
    private readonly IFileSystem _fileSystem;
    private bool _showTrayIcon;
    private bool _isStarted;
    private BitmapImage _trayIcon = _icon; // "/ContainerDesktop;component/app.ico";
    private IMenuItem _selectedMenuItem;
    private bool _updateAvailable;
    private string _updateAvailableTooltip;
    private ReleaseVersion _latestAvailableVersion;

    public MainViewModel(
        IApplicationContext applicationContext, 
        IContainerEngine containerEngine, 
        IWslService wslService, 
        IConfigurationService configurationService,
        IProcessExecutor processExecutor,
        IProductInformation productInformation,
        IFileSystem fileSystem,
        ILogger<MainViewModel> logger)
    {
        _applicationContext = applicationContext;
        _containerEngine = containerEngine;
        _containerEngine.RunningStateChanged += RunnningStateChanged;
        _wslService = wslService;
        _configurationService = configurationService;
        _processExecutor = processExecutor;
        ProductInformation = productInformation;
        _fileSystem = fileSystem;
        _logger = logger;
        OpenCommand = new DelegateCommand(Open);
        QuitCommand = new DelegateCommand(Quit);
        StartCommand = new DelegateCommand(Start, () => _containerEngine.RunningState == RunningState.Stopped);
        StopCommand = new DelegateCommand(Stop, () => _containerEngine.RunningState == RunningState.Started);
        RestartCommand = new DelegateCommand(Restart, () => _containerEngine.RunningState == RunningState.Started);
        CheckWslDistroCommand = new DelegateCommand<WslDistributionItem>(ToggleWslDistro);
        OpenDocumentationCommand = new DelegateCommand(OpenDocumentation);
        ViewLogStreamCommand = new DelegateCommand(ViewLogStream);
        CheckNetworkInterfaceCommand = new DelegateCommand<PortForwardInterface>(TogglePortForwardInterface);
        OpenSettingsCommand = new DelegateCommand(OpenSettings);
        ShowLatestReleaseCommand = new DelegateCommand(ShowLatestRelease, () => UpdateAvailable);
        ResetCommand = new DelegateCommand(Reset, () => _containerEngine.RunningState != RunningState.Starting);

        var menuItems = new List<IMenuItem>
        {
            new Category { Name = "Logs", PageType = typeof(LogsPage), Glyph = Symbol.Dictionary }
        };
        MenuItems = menuItems;
        SelectedMenuItem = menuItems[0];
        Task.Run(() => CheckForUpdateAsync());
    }

    public IContainerDesktopConfiguration Configuration => _configurationService.Configuration;

    public IProductInformation ProductInformation { get; }

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

    public DelegateCommand OpenSettingsCommand { get; }

    public ICommand QuitCommand { get; }

    public DelegateCommand StartCommand { get; }

    public DelegateCommand StopCommand { get; }

    public DelegateCommand RestartCommand { get; }

    public DelegateCommand<WslDistributionItem> CheckWslDistroCommand { get; }

    public DelegateCommand OpenDocumentationCommand { get; }

    public DelegateCommand ViewLogStreamCommand { get; }

    public DelegateCommand ShowLatestReleaseCommand { get; }

    public DelegateCommand<PortForwardInterface> CheckNetworkInterfaceCommand { get; }

    public DelegateCommand ResetCommand { get; }

    public IEnumerable<WslDistributionItem> WslDistributions =>
        _wslService.GetDistros()
            .Where(x => !_configurationService.Configuration.HiddenDistributions.Contains(x))
            .Select(x => new WslDistributionItem { Name = x, Enabled = _configurationService.Configuration.EnabledDistributions.Contains(x)});

    public IEnumerable<PortForwardInterface> NetworkInterfaces => 
        NetworkInterface.GetAllNetworkInterfaces().Where(x => x.NetworkInterfaceType != NetworkInterfaceType.Loopback && x.OperationalStatus == OperationalStatus.Up).Select(x => new PortForwardInterface(x)
        {
            Forwarded = _configurationService.Configuration.PortForwardInterfaces.Contains(x.Id)
        });

    public IEnumerable<IMenuItem> MenuItems { get; }

    public IMenuItem SelectedMenuItem
    {
        get => _selectedMenuItem;
        set => SetValueAndNotify(ref _selectedMenuItem, value);
    }

    public bool UpdateAvailable
    {
        get => _updateAvailable;
        set => SetValueAndNotify(ref _updateAvailable, value);
    }

    public string UpdateAvailableTooltip
    {
        get => _updateAvailableTooltip;
        set => SetValueAndNotify(ref _updateAvailableTooltip, value);
    }

    private void Open()
    {
        // TODO: uncomment when window has actually something to show.
        _applicationContext.ShowMainWindow();
    }

    private void OpenSettings()
    {
        _applicationContext.ShowSettings();
    }

    private void Quit()
    {
        _applicationContext.QuitApplication();
    }

    private void Stop()
    {
        Task.Run(() => SafeExecute("stop", _containerEngine.Stop));
    }

    private void Start()
    {
        Task.Run(() => SafeExecute("start", _containerEngine.Start));
    }

    private void Restart()
    {
        Task.Run(() => SafeExecute("restart", _containerEngine.Restart));
    }

    private void RunnningStateChanged(object sender, EventArgs e)
    {
        _applicationContext.InvokeOnDispatcher(() =>
        {
            IsStarted = _containerEngine.RunningState == RunningState.Started;
            StartCommand.RaiseCanExecuteChanged();
            StopCommand.RaiseCanExecuteChanged();
            RestartCommand.RaiseCanExecuteChanged();
            ResetCommand.RaiseCanExecuteChanged();
            TrayIcon = _containerEngine.RunningState switch
            {
                RunningState.Started => _runIcon,
                RunningState.Stopped => _stopIcon,
                _ => _icon
            };
        });
    }

    private void ToggleWslDistro(WslDistributionItem distro)
    {
        Task.Run(() =>
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
        });
    }

    private void TogglePortForwardInterface(PortForwardInterface portForwardInterface)
    {
        Task.Run(() =>
        {
            SafeExecute($"{(portForwardInterface.Forwarded ? "start" : "stop")} port forwarding to interface {portForwardInterface.Name}", () =>
            {
                _containerEngine.EnablePortForwardingInterface(portForwardInterface.NetworkInterface, portForwardInterface.Forwarded);
                if(portForwardInterface.Forwarded)
                {
                    if (!_configurationService.Configuration.PortForwardInterfaces.Contains(portForwardInterface.Id))
                    {
                        _configurationService.Configuration.PortForwardInterfaces.Add(portForwardInterface.Id);
                    }
                }
                else
                {
                    _configurationService.Configuration.PortForwardInterfaces.Remove(portForwardInterface.Id);
                }
                _configurationService.Save();
            });
        });
    }

    private void OpenDocumentation()
    {
        _processExecutor.Start(ProductInformation.WebSiteUrl, null, useShellExecute: true);
    }

    private void ViewLogStream()
    {
        _applicationContext.ShowMainWindow();
        var menuItem = MenuItems.OfType<Category>().FirstOrDefault(x => x.PageType == typeof(LogsPage));
        if(menuItem != null)
        {
            SelectedMenuItem = null;
            SelectedMenuItem = menuItem;
        }
    }

    private void ShowLatestRelease()
    {
        try
        {
            if (_latestAvailableVersion?.Release.HtmlUrl != null)
            {
                Process.Start(new ProcessStartInfo(_latestAvailableVersion.Release.HtmlUrl) {  UseShellExecute = true });
            }
        }
        catch
        {
            // Do nothing, this is not critical
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
            _applicationContext.InvokeOnDispatcher(() =>
                MessageBox.Show(_applicationContext.MainWindow, ex.Message, $"Failed to {caption}", MessageBoxButton.OK, MessageBoxImage.Error));
        }
    }

    private async Task CheckForUpdateAsync()
    {
        try
        {
            _logger.LogInformation("Getting the list of releases.");
            using var client = new HttpClient();
            client.DefaultRequestHeaders.UserAgent.Add(new System.Net.Http.Headers.ProductInfoHeaderValue(ProductInformation.Name, ProductInformation.Version));
            var releases = await client.GetFromJsonAsync<ReleaseInfo[]>(ProductInformation.ReleasesFeed);
            _latestAvailableVersion = releases.Select(x => new ReleaseVersion(x, SemanticVersion.Parse(x.TagName.TrimStart('v')))).OrderByDescending(x => x.SemanticVersion).FirstOrDefault();
            _applicationContext.InvokeOnDispatcher(() =>
            {
                UpdateAvailable = _latestAvailableVersion != null && _latestAvailableVersion.SemanticVersion > SemanticVersion.Parse(ProductInformation.Version);
                UpdateAvailableTooltip = UpdateAvailable ? $"There is a new version of {ProductInformation.DisplayName} available. Latest available version is {_latestAvailableVersion.SemanticVersion}." : string.Empty;
                ShowLatestReleaseCommand.RaiseCanExecuteChanged();
            });
            _logger.LogInformation("Getting the list of releases was successful. UpdateAvailable={UpdateAvailable}", UpdateAvailable);
        }
        catch(Exception ex)
        {
            _logger.LogError(ex, "Could not retrieve a list of releases.");
        }
    }

    private async void Reset()
    {
        if(MessageBox.Show(_applicationContext.MainWindow, $"This will reinstall the {ProductInformation.DisplayName} WSL2 distributions. This will remove all data. \r\nDo you want to continue ?", "Reset", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
        {
            return;
        }
        await Task.Run(() =>
        {
            SafeExecute("Reset", () =>
            {
                using (var dlg = _applicationContext.InvokeOnDispatcher(() => ProgressDialog.Show("Reset", 5)))
                {
                    LogAndProgress(dlg, "Stopping");
                    _containerEngine.Stop();
                    LogAndProgress(dlg, "Unregistering distributions");
                    _wslService.Unregister(ProductInformation.ContainerDesktopDistroName);
                    _wslService.Unregister(ProductInformation.ContainerDesktopDataDistroName);
                    LogAndProgress(dlg, "Deleting distribution folders");
                    _fileSystem.Directory.Delete(ProductInformation.ContainerDesktopDistroDir, true);
                    _fileSystem.Directory.Delete(ProductInformation.ContainerDesktopDataDistroDir, true);
                    LogAndProgress(dlg, "Importing distributions");
                    _fileSystem.Directory.CreateDirectory(ProductInformation.ContainerDesktopDistroDir);
                    _fileSystem.Directory.CreateDirectory(ProductInformation.ContainerDesktopDataDistroDir);
                    if (!_wslService.Import(ProductInformation.ContainerDesktopDistroName, ProductInformation.ContainerDesktopDistroDir, Path.Combine(ProductInformation.InstallDir, "Resources", "container-desktop-distro.tar.gz")))
                    {
                        throw new ContainerEngineException("Could not import distribution.");
                    }
                    if (!_wslService.Import(ProductInformation.ContainerDesktopDataDistroName, ProductInformation.ContainerDesktopDataDistroDir, Path.Combine(ProductInformation.InstallDir, "Resources", "container-desktop-data-distro.tar.gz")))
                    {
                        throw new ContainerEngineException("Could not import data distribution.");
                    }
                    LogAndProgress(dlg, "Starting");
                    _containerEngine.Start();
                }
                _applicationContext.InvokeOnDispatcher(() => MessageBox.Show(_applicationContext.MainWindow, $"Successfully reset {ProductInformation.DisplayName}.", "Reset"));
            });
        });

        void LogAndProgress(IProgress<string> progress, string msg)
        {
            _logger.LogInformation(msg);
            progress?.Report(msg);
        }
    }

    private record ReleaseVersion(ReleaseInfo Release, SemanticVersion SemanticVersion);
}
#pragma warning restore CA2254

