﻿namespace ContainerDesktop.Installer.ViewModels;

using ContainerDesktop.Common;
using ContainerDesktop.DesiredStateConfiguration;
using ContainerDesktop.UI.Wpf;
using ContainerDesktop.UI.Wpf.Input;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

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

    public MainViewModel(IInstallationRunner runner, IApplicationContext applicationContext, IProductInformation productInformation, ILogger<MainViewModel> logger)
    {
        ProductInformation = productInformation;
        Title = $"{ProductInformation.DisplayName} Installer ({ProductInformation.Version})";
        ShowApplyButton = true;
        ShowCloseButton = false;
        ApplyCommand = new DelegateCommand(Apply);
        CloseCommand = new DelegateCommand(Close);
        _runner = runner;
        _applicationContext = applicationContext;
        Uninstalling = runner.InstallationMode == InstallationMode.Uninstall;
        Logger = logger;
        ShowOptions = _runner.InstallationMode == InstallationMode.Install;
        if (runner.Options.AutoStart)
        {
            Apply();
        }
    }

    public IProductInformation ProductInformation { get; }

    public IEnumerable<IResource> OptionalResources => _runner.ConfigurationManifest.Resources.Where(x => x.Optional);

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
        ShowApplyButton = false;
        ShowProgress = true;
        ShowOptions = false;

        Message = $"Preparing {_runner.InstallationMode}";
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