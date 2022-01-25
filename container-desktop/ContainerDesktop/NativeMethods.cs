using System.Runtime.InteropServices;

namespace ContainerDesktop;

internal static class NativeMethods
{
    [DllImport("DnsApi", ExactSpelling = true, CharSet = CharSet.Unicode)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    public static extern int DnsQueryConfig(DNS_CONFIG_TYPE config, uint flag, string pwsAdapterName, IntPtr pReserved, IntPtr pBuffer, ref uint pBufLen);

    /// <summary>The DNS_CONFIG_TYPE enumeration provides DNS configuration type information.</summary>
    /// <remarks>
    /// <para><see href="https://docs.microsoft.com/windows/win32/api//windns/ne-windns-dns_config_type">Learn more about this API from docs.microsoft.com</see>.</para>
    /// </remarks>
    public enum DNS_CONFIG_TYPE
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
