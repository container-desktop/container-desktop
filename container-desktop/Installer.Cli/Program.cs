using ContainerDesktop.Common;
using ContainerDesktop.Common.Cli;
using ContainerDesktop.Common.Services;
using Microsoft.Extensions.DependencyInjection;
using System.IO.Abstractions;
using System.Threading.Tasks;

namespace Installer.Cli
{
    class Program : Runner
    {
        static Task<int> Main(string[] args)
        {
            return new Program().RunAsync(args, typeof(ApplyOptions));
        }

        protected override void ConfigureServices(IServiceCollection services)
        {
            services.AddTransient<IProcessor<ApplyOptions>, ApplyProcessor>();
            services.AddSingleton<IProcessExecutor, ProcessExecutor>();
            services.AddSingleton<IWslService, WslService>();
            services.AddTransient<IFileSystem, FileSystem>();
        }
    }
}
