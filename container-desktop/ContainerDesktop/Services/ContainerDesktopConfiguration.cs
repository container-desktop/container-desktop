using ContainerDesktop.Abstractions;
using ContainerDesktop.Common;
using ContainerDesktop.DesiredStateConfiguration;
using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;

namespace ContainerDesktop.Services;

public class ContainerDesktopConfiguration : ConfigurationObject
{
    private readonly IProductInformation _productInformation;
    private DnsMode _dnsMode;
    private string _dnsAddresses;

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
    [Display(Name = "DNS Mode", GroupName = "Network")]
    public DnsMode DnsMode 
    {
        get => _dnsMode;
        set => SetValueAndNotify(ref _dnsMode, value);
    }

    [Show(nameof(DnsMode), DnsMode.Static)]
    [Display(Name = "DNS Addresses", GroupName = "Network", Description = "A comma seperated list of IP addresses.", Order = 1)]
    [RequiredIf(nameof(DnsMode), DnsMode.Static)]
    [RegularExpression(@"(\b25[0-5]|\b2[0-4][0-9]|\b[01]?[0-9][0-9]?)(\.(25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)){3}(\s*,\s*(\b25[0-5]|\b2[0-4][0-9]|\b[01]?[0-9][0-9]?)(\.(25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)){3})*", ErrorMessage = "Not a valid comma separated list of IP addresses")]
    public string DnsAddresses 
    {
        get => _dnsAddresses;
        set => SetValueAndNotify(ref _dnsAddresses, value);
    }

    [JsonIgnore]
    [Display(Name = "Automatically start at login", GroupName = "Startup")]
    public bool AutoStart 
    {
        get => new AddToAutoStart { ExePath = _productInformation.AppPath }.Test(null);
        set => SetValueAndNotify(() => AutoStart, v =>
        {

            var addToAutoStart = new AddToAutoStart { ExePath = _productInformation.AppPath };
            if (v)
            {
                addToAutoStart.Set(null);
            }
            else
            {
                addToAutoStart.Unset(null);
            }

        }, value);
    }
}