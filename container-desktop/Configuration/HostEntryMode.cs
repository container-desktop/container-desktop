using System.ComponentModel.DataAnnotations;

namespace ContainerDesktop.Configuration;

public enum HostEntryMode
{
    [Display(Name = "WSL", Description = "The address off the WSL virtual network adapter is used.")]
    Wsl,
    [Display(Name = "Specific Adapter", Description = "The host entries are set to the IP address of the selected adapter.")]
    Static
}
