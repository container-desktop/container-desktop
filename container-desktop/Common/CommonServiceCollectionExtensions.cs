namespace Microsoft.Extensions.DependencyInjection;

using ContainerDesktop.Common;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.IO.Abstractions;

public static class CommonServiceCollectionExtensions
{
    public static IServiceCollection AddCommon(this IServiceCollection services)
    {
        services.TryAddSingleton<IProductInformation, ProductInformation>();
        services.TryAddSingleton<IFileSystem, FileSystem>();
        return services;
    }
}
