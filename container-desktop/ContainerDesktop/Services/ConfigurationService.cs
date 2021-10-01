using ContainerDesktop.Common;
using Newtonsoft.Json;
using System.IO.Abstractions;

namespace ContainerDesktop.Services;

public class ConfigurationService : IConfigurationService
{
    private static readonly string _configurationFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), Product.Name, "config.json");
    private readonly IFileSystem _fileSystem;

    public ConfigurationService(IFileSystem fileSystem)
    {
        _fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
        if(_fileSystem.File.Exists(_configurationFilePath))
        {
            var json = _fileSystem.File.ReadAllText(_configurationFilePath);
            Configuration = JsonConvert.DeserializeObject<ContainerDesktopConfiguration>(json);
        }
        else
        {
            Configuration = new ContainerDesktopConfiguration();
        }
    }

    public ContainerDesktopConfiguration Configuration { get; }

    public void Save()
    {
        var json = JsonConvert.SerializeObject(Configuration);
        _fileSystem.File.WriteAllText(_configurationFilePath, json);
    }
}
