using ContainerDesktop.Common;
using ContainerDesktop.Common.Cli;
using ContainerDesktop.Common.DesiredStateConfiguration;
using ContainerDesktop.Common.Input;
using System;
using System.Reactive.Threading.Tasks;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace ContainerDesktop.Installer.ViewModels
{
    public class MainViewModel : ViewModelBase, IUserInteraction
    {
        private string _title;
        private bool _showApplyButton;
        private bool _showProgress;
        private int _maxValue = int.MaxValue;
        private int _value;
        private string _message;
        private bool _uninstalling;
        private Visibility _closeButtonVisibility;
        private string _applyButtonText = "Install";
        private readonly IInstallationRunner _runner;
        private readonly IApplicationContext _applicationContext;

        public MainViewModel(IInstallationRunner runner, IApplicationContext applicationContext)
        {
            Title = $"{Product.DisplayName} Installer ({Product.Version})";
            ShowApplyButton = true;
            CloseButtonVisibility = Visibility.Hidden;
            ApplyCommand = new DelegateCommand(Apply);
            CloseCommand = new DelegateCommand(Close);
            _runner = runner;
            _applicationContext = applicationContext;
            Uninstalling = runner.InstallationMode == InstallationMode.Uninstall;
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

        public bool Uninstalling
        {
            get => _uninstalling;
            set
            {
                if(SetValueAndNotify(ref _uninstalling, value))
                {
                    ApplyButtonText = value ? "Uninstall" : "Install";
                }
            }
        }

        public Visibility CloseButtonVisibility
        {
            get => _closeButtonVisibility;
            set => SetValueAndNotify(ref _closeButtonVisibility, value);
        }

        public string ApplyButtonText
        {
            get => _applyButtonText;
            set => SetValueAndNotify(ref _applyButtonText, value);
        }

        public ICommand CloseCommand { get; }

        public ICommand ApplyCommand { get; }

        private void Apply(object parameter)
        {
            ShowApplyButton = false;
            ShowProgress = true;
            var runnerTask = Task.Run(() => _runner.RunAsync());
            runnerTask.ToObservable().Subscribe(exitCode =>
            {
                _applicationContext.ExitCode = exitCode;
                CloseButtonVisibility = Visibility.Visible;
            });
        }

        private void Close(object parameter)
        {
            _applicationContext.QuitApplication();
        }

        public bool UserConsent(string message, string caption = null)
        {
            var result = MessageBox.Show(message, caption, MessageBoxButton.YesNo);
            return result == MessageBoxResult.Yes;
        }

        public void ReportProgress(int value, int max, string message)
        {
            MaxValue = max;
            Value = value;
            Message = message;
        }
    }
}
