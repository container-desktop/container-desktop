using ContainerDesktop.Common.Cli;
using ContainerDesktop.Common.DesiredStateConfiguration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.IO.Abstractions;
using System.Threading.Tasks;

namespace Installer.Cli
{
    public class ApplyProcessor : ProcessorBase<ApplyOptions>
    {
        private readonly IServiceProvider _serviceProvider;

        public ApplyProcessor(ApplyOptions options, IServiceProvider serviceProvider, ILogger<ApplyProcessor> logger) : base(options, logger)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        }

        protected override Task ProcessCoreAsync()
        {
            var configuration = Configuration.Create(_serviceProvider, Options.ConfigurationManifestFileName, true);
            var context = new ConfigurationContext(Logger, _serviceProvider.GetRequiredService<IFileSystem>());
            configuration.Apply(context);
            return Task.CompletedTask;
        }
    }
}
