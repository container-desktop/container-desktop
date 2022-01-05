using ContainerDesktop.Common;
using ContainerDesktop.Wsl;
using IniParser;
using IniParser.Parser;
using System.ComponentModel;
using System.Net;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;

namespace ContainerDesktop.Services;

public sealed class DnsConfigurator : IDisposable
{
    private const string WslConfNetworkSection = "network";
    private const string WslConfGenerateResolvConf = "generateResolvConf";

    private readonly IWslService _wslService;
    private readonly IConfigurationService _configurationService;
    private readonly IProductInformation _productInformation;
    private readonly ILogger _logger;
    private readonly object _lock = new object();

    public DnsConfigurator(IWslService wslService, IConfigurationService configurationService, IProductInformation productInformation, ILogger logger)
    {
        _wslService = wslService ?? throw new ArgumentNullException(nameof(wslService));
        _configurationService = configurationService ?? throw new ArgumentNullException(nameof(configurationService));
        _productInformation = productInformation ?? throw new ArgumentNullException(nameof(productInformation));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        PropertyChangedEventManager.AddHandler(_configurationService.Configuration, OnDnsConfigurationChanged, nameof(ContainerDesktopConfiguration.DnsMode));
        PropertyChangedEventManager.AddHandler(_configurationService.Configuration, OnDnsConfigurationChanged, nameof(ContainerDesktopConfiguration.DnsAddresses));
        NetworkChange.NetworkAddressChanged += NetworkAddressChanged;
    }

    public void Configure()
    {
        lock (_lock)
        {
            ConfigureWslConf();
            ConfigureResolvConf();
        }
    }

    private void ConfigureWslConf()
    {
        var generateResolvConf = _configurationService.Configuration.DnsMode switch
        {
            DnsMode.Wsl => true,
            _ => false
        };
        var parser = new IniDataParser();
        var output = new StringBuilder();
        _logger.LogInformation("Reading existing wsl.conf");
        if (_wslService.ExecuteCommand("cat /etc/wsl.conf", _productInformation.ContainerDesktopDistroName, output))
        {
            var data = parser.Parse(output.ToString());
            var existingGenerateReolveConf = bool.Parse(data[WslConfNetworkSection][WslConfGenerateResolvConf] ?? bool.TrueString);
            if(generateResolvConf != existingGenerateReolveConf)
            {
                _logger.LogInformation($"Trying to set {WslConfNetworkSection}/{WslConfGenerateResolvConf}={{generateResolvConf}} in /etc/wsl.conf", generateResolvConf);
                data[WslConfNetworkSection][WslConfGenerateResolvConf] = generateResolvConf.ToString().ToLowerInvariant();
                var newContent = data.ToString().Replace(Environment.NewLine, "\n");
                _logger.LogDebug("The new wsl.conf content: {wslconf}", newContent);
                if (!_wslService.ExecuteCommand($"cat <<EOF > /etc/wsl.conf\n{newContent}\nEOF\n", _productInformation.ContainerDesktopDistroName, stdout: s => _logger.LogInformation(s), stderr: s => _logger.LogError(s)))
                {
                    _logger.LogError($"Failed to set {WslConfNetworkSection}/{WslConfGenerateResolvConf}={{generateResolvConf}} in /etc/wsl.conf", generateResolvConf);
                }
                _logger.LogInformation($"Successfully set {WslConfNetworkSection}/{WslConfGenerateResolvConf}={{generateResolvConf}} in /etc/wsl.conf", generateResolvConf);
            }
            else
            {
                _logger.LogInformation($"Skipped setting {WslConfNetworkSection}/{WslConfGenerateResolvConf}={{generateResolvConf}} in /etc/wsl.conf because it is not changed", generateResolvConf);
            }
        }
        else
        {
            _logger.LogError("Failed to read /etc/wsl.conf: {output}", output);
        }
    }

    private void ConfigureResolvConf()
    {
        var ipAddresses = _configurationService.Configuration.DnsMode switch
        {
            DnsMode.Wsl => GetWslDnsAddresses(),
            DnsMode.Auto => GetPrimaryAdapterDnsAddresses(),
            DnsMode.Static => GetStaticDnsAddresses(),
            _ => Array.Empty<string>()
        };
        if(ipAddresses.Length == 0)
        {
            _logger.LogWarning("Could not resolve DNS IP addresses for DNS Mode={dnsMode}", _configurationService.Configuration.DnsMode);
        }
        var content = string.Join('\n', ipAddresses.Select(x => $"nameserver {x}"));
        if (_wslService.ExecuteCommand($"cat <<EOF > /etc/resolv.conf\n{content}\nEOF\n", _productInformation.ContainerDesktopDistroName, stdout: s => _logger.LogInformation(s), stderr: s => _logger.LogError(s)))
        {
            _logger.LogInformation("Successfully updated /etc/resolv.conf to :{content}", content);
        }
        else
        {
            _logger.LogError($"Failed to update /etc/resolv.conf with: {content}", content);
        }
    }

    private string[] GetWslDnsAddresses()
    {
        //TODO: make it more robust
        var wslNetworkInterface = NetworkInterface.GetAllNetworkInterfaces().FirstOrDefault(x => x.Name == "vEthernet (WSL)");
        return wslNetworkInterface.GetIPProperties().UnicastAddresses.Where(x => x.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork).Select(x => x.Address.ToString()).ToArray();
    }

    private string[] GetStaticDnsAddresses()
    {
        var addresses = _configurationService.Configuration.DnsAddresses.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if(addresses.All(x => IPAddress.TryParse(x, out _)))
        {
            return addresses;
        }
        return Array.Empty<string>();
    }

    private string[] GetPrimaryAdapterDnsAddresses()
    {
        uint bufferLength = 0;
        var ret = DnsQueryConfig(DNS_CONFIG_TYPE.DnsConfigDnsServerList, 0, null, IntPtr.Zero, IntPtr.Zero, ref bufferLength);
        if (ret == 0)
        {
            var buf = Marshal.AllocHGlobal((int)bufferLength);
            try
            {
                ret = DnsQueryConfig(DNS_CONFIG_TYPE.DnsConfigDnsServerList, 0, null, IntPtr.Zero, buf, ref bufferLength);
                if (ret == 0)
                {
                    var tempBuffer = new byte[bufferLength];
                    Marshal.Copy(buf, tempBuffer, 0, (int)bufferLength);
                    var span = new ReadOnlySpan<byte>(tempBuffer);
                    var ipAddresses = new IPAddress[bufferLength / 4 - 1];
                    for (var i = 0; i < ipAddresses.Length; i++)
                    {
                        ipAddresses[i] = new IPAddress(span.Slice(4 + i * 4, 4));
                    }
                    return ipAddresses.Select(x => x.ToString()).ToArray();
                }
            }
            finally
            {
                Marshal.FreeHGlobal(buf);
            }
        }
        return Array.Empty<string>();
    }

    private void NetworkAddressChanged(object sender, EventArgs e)
    {
        Configure();
    }

    private void OnDnsConfigurationChanged(object sender, PropertyChangedEventArgs e)
    {
        Configure();
    }

    [DllImport("DnsApi", ExactSpelling = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    private static extern int DnsQueryConfig(DNS_CONFIG_TYPE config, uint flag, string pwsAdapterName, IntPtr pReserved, IntPtr pBuffer, ref uint pBufLen);

    public void Dispose()
    {
        PropertyChangedEventManager.RemoveHandler(_configurationService.Configuration, OnDnsConfigurationChanged, nameof(ContainerDesktopConfiguration.DnsMode));
        PropertyChangedEventManager.RemoveHandler(_configurationService.Configuration, OnDnsConfigurationChanged, nameof(ContainerDesktopConfiguration.DnsAddresses));
        NetworkChange.NetworkAddressChanged -= NetworkAddressChanged;
    }

    /// <summary>The DNS_CONFIG_TYPE enumeration provides DNS configuration type information.</summary>
    /// <remarks>
    /// <para><see href="https://docs.microsoft.com/windows/win32/api//windns/ne-windns-dns_config_type">Learn more about this API from docs.microsoft.com</see>.</para>
    /// </remarks>
    private enum DNS_CONFIG_TYPE
    {
        /// <summary>For use with Unicode on Windows 2000.</summary>
        DnsConfigPrimaryDomainName_W = 0,
        /// <summary>For use with ANSI on Windows 2000.</summary>
        DnsConfigPrimaryDomainName_A = 1,
        /// <summary>For use with UTF8 on Windows 2000.</summary>
        DnsConfigPrimaryDomainName_UTF8 = 2,
        /// <summary>Not currently available.</summary>
        DnsConfigAdapterDomainName_W = 3,
        /// <summary>Not currently available.</summary>
        DnsConfigAdapterDomainName_A = 4,
        /// <summary>Not currently available.</summary>
        DnsConfigAdapterDomainName_UTF8 = 5,
        /// <summary>For configuring a DNS Server list on Windows 2000.</summary>
        DnsConfigDnsServerList = 6,
        /// <summary>Not currently available.</summary>
        DnsConfigSearchList = 7,
        /// <summary>Not currently available.</summary>
        DnsConfigAdapterInfo = 8,
        /// <summary>Specifies that primary host name registration is enabled on Windows 2000.</summary>
        DnsConfigPrimaryHostNameRegistrationEnabled = 9,
        /// <summary>Specifies that adapter host name registration is enabled on Windows 2000.</summary>
        DnsConfigAdapterHostNameRegistrationEnabled = 10,
        /// <summary>Specifies configuration of the maximum number of address registrations on Windows 2000.</summary>
        DnsConfigAddressRegistrationMaxCount = 11,
        /// <summary>Specifies configuration of the host name in Unicode on Windows XP, Windows Server 2003, and later versions of Windows.</summary>
        DnsConfigHostName_W = 12,
        /// <summary>Specifies configuration of the host name in ANSI on Windows XP, Windows Server 2003, and later versions of Windows.</summary>
        DnsConfigHostName_A = 13,
        /// <summary>Specifies configuration of the host name in UTF8 on Windows XP, Windows Server 2003, and later versions of Windows.</summary>
        DnsConfigHostName_UTF8 = 14,
        /// <summary>Specifies configuration of the full host name (fully qualified domain name) in Unicode on Windows XP, Windows Server 2003, and later versions of Windows.</summary>
        DnsConfigFullHostName_W = 15,
        /// <summary>Specifies configuration of the full host name (fully qualified domain name) in ANSI on Windows XP, Windows Server 2003, and later versions of Windows.</summary>
        DnsConfigFullHostName_A = 16,
        /// <summary>Specifies configuration of the full host name (fully qualified domain name) in UTF8 on Windows XP, Windows Server 2003, and later versions of Windows.</summary>
        DnsConfigFullHostName_UTF8 = 17,
        /// <summary></summary>
        DnsConfigNameServer = 18,
    }
}
