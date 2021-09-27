namespace ContainerDesktop.Installer;

using CommandLine;
using ContainerDesktop.Common.DesiredStateConfiguration;
using Microsoft.Extensions.DependencyInjection;

public class InstallationRunner : IInstallationRunner
{
    private static readonly Type[] _verbOptions = new[] { typeof(InstallOptions), typeof(UninstallOptions) };
    private readonly ParserResult<object> _parserResult;
    private readonly IServiceProvider _serviceProvider;

    public InstallationRunner(IServiceProvider serviceProvider,
        IConfigurationManifest configurationManifest)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider)); 
        ConfigurationManifest = configurationManifest ?? throw new ArgumentNullException(nameof(configurationManifest));
        var args = GetCommandLineArgs();
        _parserResult = Parser.Default.ParseArguments(args, _verbOptions);
        InstallationMode = _parserResult.TypeInfo.Current == typeof(UninstallOptions) ? InstallationMode.Uninstall : InstallationMode.Install;
        _parserResult.WithParsed<InstallerOptions>(options => Options = options);
    }

    public IConfigurationManifest ConfigurationManifest { get; }
    
    public InstallationMode InstallationMode { get; }

    public InstallerOptions Options { get; private set; }

    public void Run(Action<ConfigurationContext> configure = null)
    {
        var logger = (ILogger)_serviceProvider.GetRequiredService<ILogger<InstallationRunner>>();
        var context = ActivatorUtilities.CreateInstance<ConfigurationContext>(_serviceProvider, logger, InstallationMode == InstallationMode.Uninstall);
        configure?.Invoke(context);
        ConfigurationManifest.Apply(context);
    }

    private string[] GetCommandLineArgs()
    {
        var args = Environment.GetCommandLineArgs();
        return args.Length > 1 ? args[1..] : new string[0];
    }
}
