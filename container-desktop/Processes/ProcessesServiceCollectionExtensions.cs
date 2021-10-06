using ContainerDesktop.Processes;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection;

public static class ProcessesServiceCollectionExtensions
{
    public static IServiceCollection AddProcessExecutor(this IServiceCollection services)
    {
        services.TryAddSingleton<IProcessExecutor, ProcessExecutor>();
        return services;
    }
}
