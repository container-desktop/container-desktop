using CommandLine;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace ContainerDesktop.Common.Cli
{
    public abstract class Runner
    {
        public Task<int> RunAsync(string[] args, params Type[] optionTypes)
        {
            return Parser.Default
                .ParseArguments(args, optionTypes)
                .MapResult(SetupAndRunAsync, _ => Task.FromResult(1));
        }

        protected virtual void ConfigureServices(IServiceCollection services)
        {
            // Do nothing
        }

        protected virtual void ConfigureLogging(ILoggingBuilder builder)
        {
            builder.AddConsole();
        }

        private Task<int> SetupAndRunAsync(object options)
        {
            var services = new ServiceCollection()
                .AddSingleton(options.GetType(), options)
                .AddOptions()
                .AddLogging(builder => ConfigureLogging(builder));
            ConfigureServices(services);
            var serviceProvider = services.BuildServiceProvider();
            var processorType = typeof(IProcessor<>).MakeGenericType(options.GetType());
            var processor = (IProcessor)serviceProvider.GetRequiredService(processorType);
            return processor.ProcessAsync();
        }
    }
}
