using CommandLine;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace ContainerDesktop.Common.Cli
{
    public abstract class Runner : IRunner
    {
        private readonly IServiceCollection _services;

        protected Runner() : this(null)
        {
        }

        protected Runner(IServiceCollection services)
        {
            _services = services ?? new ServiceCollection();
            ServiceProvider = Setup();
        }

        public IServiceProvider ServiceProvider { get; }

        public Task<int> RunAsync(string[] args, params Type[] optionTypes)
        {
            return Parser.Default
                .ParseArguments(args, optionTypes)
                .MapResult(RunAsync, _ => Task.FromResult(1));
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            (ServiceProvider as IDisposable)?.Dispose();
        }

        protected virtual void ConfigureServices(IServiceCollection services)
        {
            // Do nothing
        }

        protected virtual void ConfigureLogging(ILoggingBuilder builder)
        {
            builder.AddConsole();
        }

        private IServiceProvider Setup()
        {
            _services 
                .AddOptions()
                .AddLogging(builder => ConfigureLogging(builder))
                .AddSingleton<IRunner>(this);
            ConfigureServices(_services);
            return _services.BuildServiceProvider();
        }

        private Task<int> RunAsync(object options)
        {
            var processorType = typeof(IProcessor<>).MakeGenericType(options.GetType());
            var serviceDescriptor = _services.First(x => x.ServiceType == processorType);
            var processor = (IProcessor) ActivatorUtilities.CreateInstance(ServiceProvider, serviceDescriptor.ImplementationType, options);
            return processor.ProcessAsync();
        }
    }
}
