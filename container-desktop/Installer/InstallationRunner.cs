using CommandLine;
using ContainerDesktop.Common.Cli;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;

namespace ContainerDesktop.Installer
{
    public class InstallationRunner : Runner, IInstallationRunner
    {
        private readonly string[] _commandLineArgs;
        private static readonly Type[] _verbOptions = new[] { typeof(InstallOptions), typeof(UninstallOptions) };

        public InstallationRunner(IServiceCollection services, string[] args) : base(services)
        {
            _commandLineArgs = args;
            var parseResult = Parser.Default.ParseArguments(args, _verbOptions);
            InstallationMode = parseResult.TypeInfo.Current == typeof(UninstallOptions) ? InstallationMode.Uninstall : InstallationMode.Install;
        }

        protected override void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<IInstallationRunner>(this);
        }

        public InstallationMode InstallationMode { get; }

        public Task<int> RunAsync()
        {
            return RunAsync(_commandLineArgs, _verbOptions);
        }
    }
}
