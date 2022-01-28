using ContainerDesktop.Abstractions;
using ContainerDesktop.Common;
using Newtonsoft.Json;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace ContainerDesktop.Configuration;

public class ContainerDesktopConfiguration : ConfigurationObject, IContainerDesktopConfiguration
{
    private readonly IProductInformation _productInformation;

    public ContainerDesktopConfiguration(IProductInformation productInformation)
    {
        _productInformation = productInformation ?? throw new ArgumentNullException(nameof(productInformation));
        HiddenDistributions = new HashSet<string> { productInformation.ContainerDesktopDistroName, productInformation.ContainerDesktopDataDistroName, "docker-desktop", "docker-desktop-data" };
        Certificates = CreateObservableCollection<CertificateInfo>(nameof(Certificates));
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
    [Category(ConfigurationCategories.Basic)]
    public DnsMode DnsMode 
    {
        get => GetValue<DnsMode>();
        set => SetValueAndNotify(value);
    }

    [Show(nameof(DnsMode), DnsMode.Static)]
    [Display(Name = "DNS Addresses", GroupName = ConfigurationGroups.Network, Description = "A comma seperated list of IP addresses.", Order = 1)]
    [Category(ConfigurationCategories.Basic)]
    [RequiredIf(nameof(DnsMode), DnsMode.Static)]
    [RegularExpression(@"(\b25[0-5]|\b2[0-4][0-9]|\b[01]?[0-9][0-9]?)(\.(25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)){3}(\s*,\s*(\b25[0-5]|\b2[0-4][0-9]|\b[01]?[0-9][0-9]?)(\.(25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)){3})*", ErrorMessage = "Not a valid comma separated list of IP addresses")]
    public string? DnsAddresses 
    {
        get => GetValue<string>();
        set => SetValueAndNotify(value);
    }

    [Display(Name = "Enable Port Forwarding", GroupName = ConfigurationGroups.Network, Description = "Enables port forwarding on external network interfaces.", Order = 2)]
    [Category(ConfigurationCategories.Basic)]
    public bool PortForwardingEnabled 
    { 
        get => GetValue<bool>(); 
        set => SetValueAndNotify(value);
    }

    [JsonIgnore]
    [Display(Name = "Automatically start at login", GroupName = ConfigurationGroups.Miscellaneous)]
    [Category(ConfigurationCategories.Basic)]
    public bool AutoStart 
    {
        get => AutoStartHelper.IsAutoStartEnabled(_productInformation.AppPath);
        set => SetValueAndNotify(() => AutoStart, v => AutoStartHelper.SetAutoStart(v, _productInformation.AppPath), value);
    }

    [Display(Name = "Daemon configuration", GroupName = ConfigurationGroups.Daemon, Description = "Changing the daemon configuration may prevent the daemon from starting. If this happens replace the configuration with an empty JSON object, save and restart.")]
    [Category(ConfigurationCategories.Advanced)]
    [Json]
    [UIEditor(UIEditor.Json)]
    [RestartRequired]
    public string DaemonConfig
    {
        get => GetValue<string>() ?? "{\r\n}";
        set => SetValueAndNotify(value);
    }

    [Display(Name = "Root Certificates", GroupName = ConfigurationGroups.Daemon, Description = "Select root/intermediary CA certificates to import.")]
    [Category(ConfigurationCategories.Advanced)]
    [UIEditor(UIEditor.CheckboxList)]
    [ItemsSource(nameof(GetCertificates))]
    public ObservableCollection<CertificateInfo> Certificates { get; }

    public IEnumerable<CertificateInfo> GetCertificates() => CertificateInfo.GetCertificates();
}