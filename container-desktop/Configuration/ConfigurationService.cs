using ContainerDesktop.Abstractions;
using ContainerDesktop.Common;
using KellermanSoftware.CompareNetObjects;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System.IO;
using System.IO.Abstractions;

namespace ContainerDesktop.Configuration;

public class ConfigurationService : IConfigurationService
{
    private readonly string _configurationFilePath;
    private readonly IFileSystem _fileSystem;
    private readonly IProductInformation _productInformation;
    private ContainerDesktopConfiguration? _loadedConfiguration;
    private readonly CompareLogic _comparer;
    private readonly IApplicationContext _applicationContext;

    public ConfigurationService(IFileSystem fileSystem, IProductInformation productInformation, IApplicationContext appContext, IOptions<ConfigurationOptions> options)
    {
        _comparer = new CompareLogic(new ComparisonConfig {  MaxDifferences = int.MaxValue});
        _fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
        _productInformation = productInformation ?? throw new ArgumentNullException(nameof(productInformation));
        _applicationContext = appContext ?? throw new ArgumentNullException(nameof(appContext));
        var configOptions = options?.Value ?? new ConfigurationOptions();
        _configurationFilePath = Path.Combine(productInformation.ContainerDesktopAppDataDir, "config.json");
        Configuration = new ContainerDesktopConfiguration(productInformation);
        if (_fileSystem.File.Exists(_configurationFilePath))
        {
            Load(false);
        }
        else if(configOptions.SaveOnInitialize)
        {
            SaveAndNotify(false);
        }
    }

    public bool IsChanged() => !_comparer.Compare(_loadedConfiguration, Configuration).AreEqual;
    
    public event EventHandler<ConfigurationChangedEventArgs>? ConfigurationChanged;

    public IContainerDesktopConfiguration Configuration { get; }

    public void Load() => Load(true);

    public void Save() => SaveAndNotify(true);

    public void SaveAndNotify(bool notify)
    {
        if (Configuration.IsValid)
        {
            var result = _comparer.Compare(_loadedConfiguration, Configuration);
            if (!result.AreEqual)
            {
                var json = JsonConvert.SerializeObject(Configuration, Formatting.Indented);
                _fileSystem.File.WriteAllText(_configurationFilePath, json);
                if (notify)
                {
                    var changedProperties = result.Differences.Select(x => x.PropertyName).ToArray();
                    var restartRequested = Configuration.GetType().GetProperties().Any(x => changedProperties.Contains(x.Name) && x.IsDefined(typeof(RestartRequiredAttribute), true));
                    ConfigurationChanged?.Invoke(this, new ConfigurationChangedEventArgs(restartRequested, changedProperties));
                }
                _loadedConfiguration = new ContainerDesktopConfiguration(_productInformation);
                JsonConvert.PopulateObject(json, _loadedConfiguration);
            }
        }
    }

    protected void Load(bool notify)
    {
        var json = _fileSystem.File.ReadAllText(_configurationFilePath);
        var loaded = new ContainerDesktopConfiguration(_productInformation);
        JsonConvert.PopulateObject(json, loaded);
        var result = _comparer.Compare(Configuration, loaded);
        if(!result.AreEqual)
        {
            JsonConvert.PopulateObject(json, Configuration);
            if (notify)
            {
                var changedProperties = result.Differences.Select(x => x.PropertyName).ToArray();
                ConfigurationChanged?.Invoke(this, new ConfigurationChangedEventArgs(false, changedProperties));
            }
        }
        _loadedConfiguration = loaded;
    }
}
