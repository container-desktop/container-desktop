using ContainerDesktop.Common.DesiredStateConfiguration;
using ContainerDesktop.Installer.ViewModels;

namespace ContainerDesktop.Installer;

public class InstallerUserInteraction : IUserInteraction
{
    private readonly MainViewModel _mainViewModel;
    private readonly IInstallationRunner _installationRunner;

    public InstallerUserInteraction(MainViewModel mainViewModel, IInstallationRunner runner)
    {
        _mainViewModel = mainViewModel;
        _installationRunner = runner;
    }

    public bool Uninstalling
    {
        get => _mainViewModel.Uninstalling;
        set => _mainViewModel.Uninstalling = value;
    }

    public void ReportProgress(int value, int max, string message, string extraInformation = null)
    {
        _mainViewModel.ReportProgress(value, max, message, extraInformation);   
    }

    public bool UserConsent(string message, string caption = null)
    {
        if (_installationRunner.Options.Quiet || _installationRunner.Options.Unattended)
        {
            return true;
        }
        return _mainViewModel.UserConsent(message, caption);
    }
}
