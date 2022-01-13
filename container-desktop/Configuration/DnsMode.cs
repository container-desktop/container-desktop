using System.ComponentModel.DataAnnotations;

namespace ContainerDesktop.Configuration;

public enum DnsMode
{
    [Display(Name = "WSL", Description = "WSL handles DNS.")]
    Wsl,
    [Display(Name = "Primary network adapter", Description = "Container Desktop sets the DNS server to the DNS server address(es) of the primary network adapter.")]
    Auto,
    [Display(Name = "Static", Description = "DNS is set to a list of static IP addresses.")]
    Static
}
