namespace ContainerDesktop.DesiredStateConfiguration;

using ContainerDesktop.Common;
using Newtonsoft.Json;
using System.IO.Abstractions;

public class ConfigurationContext
{
    private readonly IUserInteraction _userInteraction;
    private readonly IApplicationContext _applicationContext;
    private readonly bool _uninstall;

    public ConfigurationContext(
        bool uninstall, 
        ILogger logger, 
        IFileSystem fileSystem, 
        IApplicationContext applicationContext, 
        IProductInformation productInformation,
        IUserInteraction userInteraction = null)
    {
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        FileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
        _applicationContext = applicationContext ?? throw new ArgumentNullException(nameof(applicationContext));
        ProductInformation = productInformation ?? throw new ArgumentNullException(nameof(productInformation));
        _userInteraction = userInteraction;
        Uninstall = uninstall;
        LoadState();
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

    public bool Updating => !string.IsNullOrEmpty(InstalledVersion) && InstalledVersion != ProductInformation.Version;

    public ILogger Logger { get; }

    public IFileSystem FileSystem { get; }

    public IProductInformation ProductInformation {  get; }

    public Dictionary<string, object> State { get; } = new Dictionary<string, object>();

    public bool RestartPending { get; set; }

    public bool DelayReboot { get; set; }

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
        return new ConfigurationContext(uninstall, Logger, FileSystem, _applicationContext, ProductInformation, _userInteraction);
    }

    public void LoadState()
    {
        var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), ProductInformation.Name, "installer-state.json");
        if(FileSystem.File.Exists(path))
        {
            var json = FileSystem.File.ReadAllText(path);
            JsonConvert.PopulateObject(json, State);
        }
    }

    public void SaveState()
    {
        var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), ProductInformation.Name, "installer-state.json");
        var directory = Path.GetDirectoryName(path);
        if(!Directory.Exists(directory))
        {
            FileSystem.Directory.CreateDirectory(directory);
        }
        var json = JsonConvert.SerializeObject(State, Formatting.Indented);
        FileSystem.File.WriteAllText(path, json);
    }

    public void ClearState()
    {
        var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), ProductInformation.Name, "installer-state.json");
        if (FileSystem.File.Exists(path))
        {
            FileSystem.File.Delete(path);
        }
        State.Clear();
    }
}
