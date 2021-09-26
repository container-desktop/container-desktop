namespace ContainerDesktop.Common.DesiredStateConfiguration;

using System.IO.Abstractions;

public class ConfigurationContext
{
    private readonly IUserInteraction _userInteraction;
    private readonly IApplicationContext _applicationContext;
    private readonly bool _uninstall;

    public ConfigurationContext(bool uninstall, ILogger logger, IFileSystem fileSystem, IApplicationContext applicationContext, IUserInteraction userInteraction = null)
    {
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        FileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
        _applicationContext = applicationContext ?? throw new ArgumentNullException(nameof(applicationContext));
        _userInteraction = userInteraction;
        Uninstall = uninstall;
    }

    public bool Uninstall
    {
        get => _uninstall;
        init
        {
            _uninstall = value;
            if (_userInteraction != null)
            {
                _userInteraction.Uninstalling = value;
            }
        }
    }

    public string InstalledVersion { get; set; }

    public ILogger Logger { get; }

    public IFileSystem FileSystem { get; }

    public bool AskUserConsent(string message, string caption = null)
    {
        return _userInteraction?.UserConsent(message, caption) ?? true;
    }

    public void ReportProgress(int value, int max, string message, string extraInformation = null)
    {
        _userInteraction?.ReportProgress(value, max, message, extraInformation);
        Logger.LogInformation(message);
    }

    public void QuitApplication()
    {
        _applicationContext.QuitApplication();
    }

    public ConfigurationContext WithUninstall(bool uninstall)
    {
        return new ConfigurationContext(uninstall, Logger, FileSystem, _applicationContext, _userInteraction);
    }
}
