using ContainerDesktop.Abstractions;
using ContainerDesktop.Common;
using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;

namespace ContainerDesktop.Configuration;

public class ContainerDesktopConfiguration : ConfigurationObject, IContainerDesktopConfiguration
{
    private readonly IProductInformation _productInformation;

    public ContainerDesktopConfiguration(IProductInformation productInformation)
    {
        _productInformation = productInformation ?? throw new ArgumentNullException(nameof(productInformation));
        HiddenDistributions = new HashSet<string> { productInformation.ContainerDesktopDistroName, productInformation.ContainerDesktopDataDistroName, "docker-desktop", "docker-desktop-data" };
    }

    [Hide]
    public HashSet<string> EnabledDistributions { get; } = new HashSet<string>();
    [Hide]
    [JsonIgnore]
    public HashSet<string> HiddenDistributions { get; }
    [Hide]
    public HashSet<string> PortForwardInterfaces { get; } = new HashSet<string>();
    
    [UIEditor(UIEditor.RadioList)]
    [Display(Name = "DNS Mode", GroupName = ConfigurationGroups.Network)]
    public DnsMode DnsMode 
    {
        get => GetValue<DnsMode>();
        set => SetValueAndNotify(value);
    }

    [Show(nameof(DnsMode), DnsMode.Static)]
    [Display(Name = "DNS Addresses", GroupName = ConfigurationGroups.Network, Description = "A comma seperated list of IP addresses.", Order = 1)]
    [RequiredIf(nameof(DnsMode), DnsMode.Static)]
    [RegularExpression(@"(\b25[0-5]|\b2[0-4][0-9]|\b[01]?[0-9][0-9]?)(\.(25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)){3}(\s*,\s*(\b25[0-5]|\b2[0-4][0-9]|\b[01]?[0-9][0-9]?)(\.(25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)){3})*", ErrorMessage = "Not a valid comma separated list of IP addresses")]
    public string? DnsAddresses 
    {
        get => GetValue<string>();
        set => SetValueAndNotify(value);
    }

    [Display(Name = "Enable Port Forwarding", GroupName = ConfigurationGroups.Network, Description = "Enables port forwarding on external network interfaces.", Order = 2)]
    public bool PortForwardingEnabled 
    { 
        get => GetValue<bool>(); 
        set => SetValueAndNotify(value);
    }

    [JsonIgnore]
    [Display(Name = "Automatically start at login", GroupName = ConfigurationGroups.Miscellaneous)]
    public bool AutoStart 
    {
        get => AutoStartHelper.IsAutoStartEnabled(_productInformation.AppPath);
        set => SetValueAndNotify(() => AutoStart, v => AutoStartHelper.SetAutoStart(v, _productInformation.AppPath), value);
    }
}