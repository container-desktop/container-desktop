using ContainerDesktop.Common;
using Newtonsoft.Json;
using System.IO.Abstractions;

namespace ContainerDesktop.Services;

public class ConfigurationService : IConfigurationService
{
    private readonly string _configurationFilePath;
    private readonly IFileSystem _fileSystem;

    public ConfigurationService(IFileSystem fileSystem, IProductInformation productInformation)
    {
        _fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
        if(productInformation == null)
        {
            throw new ArgumentNullException(nameof(productInformation));
        }
        _configurationFilePath = Path.Combine(productInformation.ContainerDesktopAppDataDir, "config.json");
        Configuration = new ContainerDesktopConfiguration(productInformation);
        if (_fileSystem.File.Exists(_configurationFilePath))
        {
            var json = _fileSystem.File.ReadAllText(_configurationFilePath);
            JsonConvert.PopulateObject(json, Configuration);
        }
    }

    public ContainerDesktopConfiguration Configuration { get; }

    public void Save()
    {
        var json = JsonConvert.SerializeObject(Configuration);
        _fileSystem.File.WriteAllText(_configurationFilePath, json);
    }
}
