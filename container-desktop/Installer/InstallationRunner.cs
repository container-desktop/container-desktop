namespace ContainerDesktop.Installer;

using CommandLine;
using ContainerDesktop.Common.Cli;
using Microsoft.Extensions.DependencyInjection;


public class InstallationRunner : Runner, IInstallationRunner
{
    private readonly string[] _commandLineArgs;
    private static readonly Type[] _verbOptions = new[] { typeof(InstallOptions), typeof(UninstallOptions) };

    public InstallationRunner(IServiceCollection services, string[] args) : base(services)
    {
        _commandLineArgs = args;
        var parseResult = Parser.Default.ParseArguments(args, _verbOptions);
        InstallationMode = parseResult.TypeInfo.Current == typeof(UninstallOptions) ? InstallationMode.Uninstall : InstallationMode.Install;
        parseResult.WithParsed<InstallerOptions>(options => Options = options);
    }

    protected override void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<IInstallationRunner>(this);
    }

    public InstallationMode InstallationMode { get; }

    public InstallerOptions Options { get; private set; }

    public Task<int> RunAsync()
    {
        return RunAsync(_commandLineArgs, _verbOptions);
    }
}
