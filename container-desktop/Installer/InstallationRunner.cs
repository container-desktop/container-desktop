namespace ContainerDesktop.Installer;

using CommandLine;
using ContainerDesktop.Common.DesiredStateConfiguration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

public class InstallationRunner : IInstallationRunner
{
    private readonly IServiceProvider _serviceProvider;

    public InstallationRunner(
        IServiceProvider serviceProvider,
        IConfigurationManifest configurationManifest,
        IOptions<InstallerOptions> options)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider)); 
        ConfigurationManifest = configurationManifest ?? throw new ArgumentNullException(nameof(configurationManifest));
        Options = options?.Value ?? throw new ArgumentNullException(nameof(options));
    }

    public IConfigurationManifest ConfigurationManifest { get; }
    
    public InstallationMode InstallationMode => Options is UninstallOptions ? InstallationMode.Uninstall : InstallationMode.Install;

    public InstallerOptions Options { get; private set; }

    public ConfigurationResult Run(Action<ConfigurationContext> configure = null)
    {
        var logger = (ILogger)_serviceProvider.GetRequiredService<ILogger<InstallationRunner>>();
        var context = ActivatorUtilities.CreateInstance<ConfigurationContext>(_serviceProvider, logger, InstallationMode == InstallationMode.Uninstall);
        configure?.Invoke(context);
        context.DelayReboot = Options.Unattended;
        return ConfigurationManifest.Apply(context);
    }

    private InstallerOptions PostConfigureOptions(InstallerOptions options)
    {
        if(options.Quiet)
        {
            options.AutoStart = true;
        }
        if(options.Unattended)
        {
            options.AutoStart = true;
            options.Quiet = true;
        }
        return options;
    }
}
