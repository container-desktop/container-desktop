using ContainerDesktop.Configuration;

namespace Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddConfiguration(this IServiceCollection services, Action<ConfigurationOptions>? configure = null)
    {
        if(configure != null)
        {
            services.Configure(configure);
        }
        services.AddSingleton<IConfigurationService, ConfigurationService>();
        return services.AddSingleton<IContainerDesktopConfiguration>(sp => sp.GetRequiredService<IConfigurationService>().Configuration);
    }
}
