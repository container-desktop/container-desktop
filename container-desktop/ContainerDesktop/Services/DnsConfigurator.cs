﻿using ContainerDesktop.Common;
using ContainerDesktop.Configuration;
using ContainerDesktop.Wsl;
using System.Net;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;

namespace ContainerDesktop.Services;

#pragma warning disable CA2254

public sealed class DnsConfigurator : IDisposable
{
    private const string DnsHostAddress = "192.168.33.1";

    private readonly IWslService _wslService;
    private readonly IConfigurationService _configurationService;
    private readonly IProductInformation _productInformation;
    private readonly ILogger _logger;
    private readonly object _lock = new();
    private Task _dnsForwarderTask = null;
    private CancellationTokenSource _dnsForwarderCts = new();

    public DnsConfigurator(IWslService wslService, IConfigurationService configurationService, IProductInformation productInformation, ILogger logger)
    {
        _wslService = wslService ?? throw new ArgumentNullException(nameof(wslService));
        _configurationService = configurationService ?? throw new ArgumentNullException(nameof(configurationService));
        _productInformation = productInformation ?? throw new ArgumentNullException(nameof(productInformation));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        ConfigurationChangedEventManager.AddHandler(_configurationService, OnConfigurationChanged);
        NetworkChange.NetworkAddressChanged += NetworkAddressChanged;
        Configure();
    }

    public void Configure()
    {
        lock (_lock)
        {
            ConfigureResolvConf();
            StartDnsForwarder();
        }
    }

    public void Dispose()
    {
        ConfigurationChangedEventManager.RemoveHandler(_configurationService, OnConfigurationChanged);
        NetworkChange.NetworkAddressChanged -= NetworkAddressChanged;
        StopDnsForwarder();
    }

    private void StopDnsForwarder()
    {
        _wslService.ExecuteCommand("pkill -TERM -x dns-forwarder", _productInformation.ContainerDesktopDistroName);
        if(_dnsForwarderTask != null)
        {
            try
            {
                _dnsForwarderCts.Cancel();
                _dnsForwarderTask.Wait(5000);
            }
            catch
            {
                // Do nothing, this is expected.
            }
        }
    }

    private void StartDnsForwarder()
    {
        var ipAddresses = _configurationService.Configuration.DnsMode switch
        {
            DnsMode.Wsl => GetWslDnsAddresses(),
            DnsMode.Auto => GetPrimaryAdapterDnsAddresses(),
            DnsMode.Static => GetStaticDnsAddresses(),
            _ => Array.Empty<string>()
        };
        if (ipAddresses.Length == 0)
        {
            _logger.LogWarning("Could not resolve DNS IP addresses for DNS Mode={dnsMode}", _configurationService.Configuration.DnsMode);
        }
        else
        {
            StopDnsForwarder();
            _dnsForwarderCts = new CancellationTokenSource();
            if(!_wslService.ExecuteCommand($"ip addr show eth0 | grep {DnsHostAddress}", _productInformation.ContainerDesktopDistroName))
            {
                _wslService.ExecuteCommand($"ip addr add {DnsHostAddress}/ 24 dev eth0", _productInformation.ContainerDesktopDistroName);
            }
            var nameservers = string.Join(',', ipAddresses);
            _dnsForwarderTask = Task.Run(() => _wslService.ExecuteCommandAsync($"dns-forwarder -l {DnsHostAddress}:53 -n {nameservers}", _productInformation.ContainerDesktopDistroName, stdout: s => _logger.LogInformation(s), stderr: s => _logger.LogError(s), cancellationToken: _dnsForwarderCts.Token));
            _logger.LogInformation("Started DNS forwarder for name servers {NameServers}.", nameservers);
        }
    }

    private void ConfigureResolvConf()
    {
        if (_wslService.ExecuteCommand($"cat <<EOF > /etc/resolv.conf\nnameserver {DnsHostAddress}\nEOF\n", _productInformation.ContainerDesktopDistroName, stdout: s => _logger.LogInformation(s), stderr: s => _logger.LogError(s)))
        {
            _logger.LogInformation("Successfully updated /etc/resolv.conf to: nameserver {DnsHostAddress}", DnsHostAddress);
        }
        else
        {
            _logger.LogError("Failed to update /etc/resolv.conf with: nameserver {DnsHostAddress}", DnsHostAddress);
        }
    }

    private static string[] GetWslDnsAddresses()
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

    private static string[] GetPrimaryAdapterDnsAddresses()
    {
        uint bufferLength = 0;
        var ret = NativeMethods.DnsQueryConfig(NativeMethods.DNS_CONFIG_TYPE.DnsConfigDnsServerList, 0, null, IntPtr.Zero, IntPtr.Zero, ref bufferLength);
        if (ret == 0)
        {
            var buf = Marshal.AllocHGlobal((int)bufferLength);
            try
            {
                ret = NativeMethods.DnsQueryConfig(NativeMethods.DNS_CONFIG_TYPE.DnsConfigDnsServerList, 0, null, IntPtr.Zero, buf, ref bufferLength);
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

    private void OnConfigurationChanged(object sender, ConfigurationChangedEventArgs e)
    {
        if (e.PropertiesChanged.Any(x => x.StartsWith("Dns")))
        {
            Configure();
        }
    }
}

#pragma warning restore CA2254
