using System.ComponentModel.DataAnnotations;

namespace ContainerDesktop.Configuration;

public enum HostEntryMode
{
    [Display(Name = "WSL", Description = "The address off the WSL virtual network adapter is used.")]
    Wsl,
    [Display(Name = "Automatic", Description = "Container Desktop tries to set the host entries to the first online non virtual adapter.")]
    Auto,
    [Display(Name = "Specific Adapter", Description = "The host entries are set to the IP address of the selected adapter.")]
    Static
}
