namespace ContainerDesktop.Installer;

using ContainerDesktop.Common.DesiredStateConfiguration;
using System.IO.Abstractions;

public class InstallProcessor : ConfigurationManifestProcessor<InstallOptions>
{
    public InstallProcessor(
        InstallOptions options,
        IConfigurationManifest configurationManifest,
        IFileSystem fileSystem,
        IUserInteraction userInteraction,
        IApplicationContext applicationContext,
        ILogger<InstallProcessor> logger) : base(options, configurationManifest, fileSystem, userInteraction, applicationContext, logger)
    {
    }

    protected override ConfigurationContext CreateContext()
    {
        return new ConfigurationContext(Logger, FileSystem, ApplicationContext, UserInteraction);
    }
}
