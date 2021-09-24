namespace ContainerDesktop.Installer;

using ContainerDesktop.Common;
using ContainerDesktop.Common.DesiredStateConfiguration;
using System.IO.Abstractions;

public class UninstallProcessor : ConfigurationManifestProcessor<UninstallOptions>
{
    public UninstallProcessor(
        UninstallOptions options,
        IConfigurationManifest configurationManifest,
        IFileSystem fileSystem,
        IUserInteraction userInteraction,
        IApplicationContext applicationContext,
        ILogger<InstallProcessor> logger) : base(options, configurationManifest, fileSystem, userInteraction, applicationContext, logger)
    {
    }

    protected override ConfigurationContext CreateContext()
    {
        return new ConfigurationContext(Logger, FileSystem, ApplicationContext, UserInteraction)
        {
            Uninstall = true
        };
    }
}
