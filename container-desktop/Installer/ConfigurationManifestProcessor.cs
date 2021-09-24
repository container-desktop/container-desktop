namespace ContainerDesktop.Installer;

using ContainerDesktop.Common;
using ContainerDesktop.Common.Cli;
using ContainerDesktop.Common.DesiredStateConfiguration;
using System.IO.Abstractions;


public abstract class ConfigurationManifestProcessor<TOptions> : ProcessorBase<TOptions>
{
    protected ConfigurationManifestProcessor(
        TOptions options,
        IConfigurationManifest configurationManifest,
        IFileSystem fileSystem,
        IUserInteraction userInteraction,
        IApplicationContext applicationContext,
        ILogger logger) : base(options, logger)
    {
        ConfigurationManifest = configurationManifest ?? throw new ArgumentNullException(nameof(configurationManifest));
        FileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
        ApplicationContext = applicationContext ?? throw new ArgumentNullException(nameof(applicationContext));
        UserInteraction = userInteraction ?? throw new ArgumentNullException(nameof(userInteraction));
    }

    public IConfigurationManifest ConfigurationManifest { get; }

    public IFileSystem FileSystem { get; }

    public IUserInteraction UserInteraction { get; }

    public IApplicationContext ApplicationContext { get; }

    protected abstract ConfigurationContext CreateContext();

    protected override Task ProcessCoreAsync()
    {
        var context = CreateContext();
        ConfigurationManifest.Apply(context);
        return Task.CompletedTask;
    }
}
