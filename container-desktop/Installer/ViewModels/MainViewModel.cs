using ContainerDesktop.Abstractions;
using ContainerDesktop.Common;
using ContainerDesktop.Configuration;
using ContainerDesktop.DesiredStateConfiguration;
using ContainerDesktop.UI.Wpf.Input;
using Newtonsoft.Json;
using System.IO.Abstractions;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Reflection;
using System.Windows;
using System.Windows.Input;

namespace ContainerDesktop.Installer.ViewModels;

#pragma warning disable CA2254

public class MainViewModel : NotifyObject, IUserInteraction
{
    private string _title;
    private bool _showApplyButton;
    private bool _showProgress;
    private int _maxValue = int.MaxValue;
    private int _value;
    private string _message;
    private string _extraInformation;
    private bool _uninstalling;
    private bool _showOptions;
    private bool _showCloseButton;
    private string _applyButtonText = "Install";
    private readonly IInstallationRunner _runner;
    private readonly IApplicationContext _applicationContext;
    private readonly IConfigurationService _configurationService;
    private readonly IFileSystem _fileSystem;

    public MainViewModel(
        IFileSystem fileSystem,
        IInstallationRunner runner, 
        IApplicationContext applicationContext, 
        IProductInformation productInformation, 
        IConfigurationService configurationService,
        ILogger<MainViewModel> logger)
    {
        _fileSystem = fileSystem;
        ProductInformation = productInformation;
        Title = $"{ProductInformation.DisplayName} Installer ({ProductInformation.Version})";
        ShowApplyButton = true;
        ShowCloseButton = false;
        ApplyCommand = new DelegateCommand(Apply);
        CloseCommand = new DelegateCommand(Close);
        _runner = runner;
        _applicationContext = applicationContext;
        _configurationService = configurationService;
        Uninstalling = runner.InstallationMode == InstallationMode.Uninstall;
        Logger = logger;
        ShowOptions = _runner.InstallationMode == InstallationMode.Install;
        OptionalResources = GetOptionalResources();
        if (runner.Options.AutoStart)
        {
            Apply();
        }
    }

    private IEnumerable<IResource> GetOptionalResources()
    {
        var optionalResources = _runner.ConfigurationManifest.Resources.Where(x => x.Optional).ToList();
        var optionsFileName = Path.Combine(ProductInformation.ContainerDesktopAppDataDir, "installer-optionals.json");
        if(_fileSystem.File.Exists(optionsFileName))
        {
            var json = _fileSystem.File.ReadAllText(optionsFileName);
            var optionals = JsonConvert.DeserializeObject<string[]>(json);
            foreach(var resource in optionalResources.OfType<ResourceBase>())
            {
                resource.Enabled = optionals.Contains(resource.Id);
            }
        }
        return optionalResources;
    }

    private void SaveOptionalResources()
    {
        var optionsFileName = Path.Combine(ProductInformation.ContainerDesktopAppDataDir, "installer-optionals.json");
        if(!_fileSystem.Directory.Exists(ProductInformation.ContainerDesktopAppDataDir))
        {
            _fileSystem.Directory.CreateDirectory(ProductInformation.ContainerDesktopAppDataDir);
        }
        var optionals = OptionalResources.Where(x => x.Enabled).Select(x => x.Id).ToList();
        var json = JsonConvert.SerializeObject(optionals);
        _fileSystem.File.WriteAllText(optionsFileName, json);
    }

    public IProductInformation ProductInformation { get; }

    public IEnumerable<IResource> OptionalResources { get; }

    public bool ShowOptions
    {
        get => _showOptions;
        set => SetValueAndNotify(ref _showOptions, value);
    }

    public string Title
    {
        get => _title;
        set => SetValueAndNotify(ref _title, value);
    }

    public bool ShowApplyButton
    {
        get => _showApplyButton;
        set => SetValueAndNotify(ref _showApplyButton, value);
    }

    public bool ShowProgress
    {
        get => _showProgress;
        set => SetValueAndNotify(ref _showProgress, value);
    }

    public int MaxValue
    {
        get => _maxValue;
        set => SetValueAndNotify(ref _maxValue, value);
    }

    public int Value
    {
        get => _value;
        set => SetValueAndNotify(ref _value, value);
    }

    public string Message
    {
        get => _message;
        set => SetValueAndNotify(ref _message, value);
    }

    public string ExtraInformation
    {
        get => _extraInformation;
        set => SetValueAndNotify(ref _extraInformation, value);
    }

    public bool Uninstalling
    {
        get => _uninstalling;
        set
        {
            if (SetValueAndNotify(ref _uninstalling, value))
            {
                ApplyButtonText = value ? "Uninstall" : "Install";
            }
        }
    }

    public bool ShowCloseButton
    {
        get => _showCloseButton;
        set => SetValueAndNotify(ref _showCloseButton, value);
    }

    public string ApplyButtonText
    {
        get => _applyButtonText;
        set => SetValueAndNotify(ref _applyButtonText, value);
    }

    public ICommand CloseCommand { get; }

    public ICommand ApplyCommand { get; }

    public ILogger<MainViewModel> Logger { get; }

    private void Apply()
    {
        if (Uninstalling && !_runner.Options.Quiet)
        {
            if (MessageBox.Show("Uninstalling Container Desktop will remove the container-desktop-data WSL2 distribution.\r\nThis will destroy all Docker containers, images, volumes, and removes the files generated by the containers/applications on the local machine.If you want to upgrade to a newer Container Desktop version, just run the installer to upgrade without loosing any data.\r\n\r\nDo you want to continue ?",
                            "Uninstall", MessageBoxButton.YesNo) == MessageBoxResult.No)
            {
                return;
            }
        }

        SaveOptionalResources();

        ShowApplyButton = false;
        ShowProgress = true;
        ShowOptions = false;

        Message = $"Preparing {_runner.InstallationMode}";

        SetConfigurationSettings();

        var runnerTask = Task.Run(() => _runner.Run());
        runnerTask.ToObservable().Subscribe(result =>
        {
            _applicationContext.InvokeOnDispatcher(() =>
            {
                _applicationContext.ExitCode = result switch
                {
                    ConfigurationResult.Succeeded => 0,
                    ConfigurationResult.PendingRestart => 3010,
                    _ => 1
                };
                if (_runner.Options.Quiet)
                {
                    _applicationContext.QuitApplication();
                }
                else
                {
                    ShowCloseButton = true;
                }
            });
        }, ex =>
        {
            Logger.LogError(ex, ex.Message);
            _applicationContext.InvokeOnDispatcher(() =>
            {
                if (_runner.Options.Quiet)
                {
                    _applicationContext.ExitCode = 1;
                    _applicationContext.QuitApplication();
                }
                else
                {
                    MessageBox.Show($"{_runner.InstallationMode}ing failed. Please view the event log for possible errors.\r\n\r\nError message: {ex.Message}", $"{_runner.InstallationMode} failed", MessageBoxButton.OK);
                    ShowCloseButton = true;
                }
            });
        });
    }

    private void SetConfigurationSettings()
    {
        foreach(var kv in _runner.Options.Settings)
        {
            var parts = kv.Split('=');
            if(parts.Length == 2)
            {
                var propInfo = typeof(IContainerDesktopConfiguration).GetProperty(parts[0], BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
                if(propInfo != null)
                {
                    var value = ConvertValueHelper.ConvertFrom(propInfo.PropertyType, parts[1]);
                    propInfo.SetValue(_configurationService.Configuration, value);
                }
            }
        }
    }

    private void Close()
    {
        _applicationContext.QuitApplication();
    }

    public bool UserConsent(string message, string caption = null)
    {
        if (_runner.Options.Quiet)
        {
            Logger.LogInformation("[Quiet] UserConsent {Caption}: {Message} answered with yes.", caption, message);
            return true;
        }
        else
        {
            var result = MessageBox.Show(message, caption, MessageBoxButton.YesNo);
            Logger.LogInformation("UserConsent {Caption}: {Message} answered with {Answer}.", caption, message, result);
            return result == MessageBoxResult.Yes;
        }
    }

    public void ReportProgress(int value, int max, string message, string extraInformation = null)
    {
        MaxValue = max;
        Value = value;
        Message = message;
        ExtraInformation = extraInformation ?? string.Empty;
        Logger.LogInformation("[{ProgressAt}/{ProgressMax}] {ProgressMessage}. {ProgressExtraInformation}", value, max, message, extraInformation);
    }
}
#pragma warning restore CA2254

