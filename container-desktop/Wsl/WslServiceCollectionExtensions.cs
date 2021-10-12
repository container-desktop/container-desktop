using ContainerDesktop.Wsl;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection;

public static class WslServiceCollectionExtensions
{
    public static IServiceCollection AddWsl(this IServiceCollection services)
    {
        services.AddProcessExecutor();
        services.TryAddSingleton<IWslService, WslService>();
        return services;
    }
}

