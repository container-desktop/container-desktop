using ContainerDesktop.Abstractions;

namespace ContainerDesktop.Configuration;

public interface IContainerDesktopConfiguration : IConfigurationObject
{
    HashSet<string> EnabledDistributions { get; }
    HashSet<string> HiddenDistributions { get; }
    HashSet<string> PortForwardInterfaces { get; }
    DnsMode DnsMode { get; set; }
    string? DnsAddresses { get; set; }
    bool AutoStart { get; set; }
    bool PortForwardingEnabled { get; set; }
}
