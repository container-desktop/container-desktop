using ContainerDesktop.Common;

namespace ContainerDesktop.Services;

public class ContainerDesktopConfiguration
{
    public ContainerDesktopConfiguration(IProductInformation productInformation)
    {
        HiddenDistributions = new List<string> { productInformation.ContainerDesktopDistroName, productInformation.ContainerDesktopDataDistroName, "docker-desktop", "docker-desktop-data" };
    }

    public List<string> EnabledDistributions { get; } = new List<string>();
    public List<string> HiddenDistributions { get; }
    public List<string> PortForwardInterfaces { get; } = new List<string>();
}