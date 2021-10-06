namespace ContainerDesktop.DesiredStateConfiguration;

public class FileConfigurationManifest : ConfigurationManifest
{
    public FileConfigurationManifest(string fileName, IServiceProvider serviceProvider)
        : base(serviceProvider, File.OpenRead(fileName), new Uri(fileName))
    {
    }
}
