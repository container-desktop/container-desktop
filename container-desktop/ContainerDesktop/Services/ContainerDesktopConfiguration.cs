using ContainerDesktop.Common;

namespace ContainerDesktop.Services;

public class ContainerDesktopConfiguration
{
    public List<string> EnabledDistributions { get; } = new List<string>();
    public List<string> HiddenDistributions { get; } = new List<string> { Product.ContainerDesktopDistroName, Product.ContainerDesktopDataDistroName, "docker-desktop", "docker-desktop-data" };
}