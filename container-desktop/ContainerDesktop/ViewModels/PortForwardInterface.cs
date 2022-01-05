using ContainerDesktop.Abstractions;
using System.Net;
using System.Net.NetworkInformation;

namespace ContainerDesktop.ViewModels;

public class PortForwardInterface : NotifyObject
{
    private readonly NetworkInterface _networkInterface;
    private string _name;
    private bool _forwarded;
    private bool _enabled;

    public PortForwardInterface(NetworkInterface networkInterface)
    {
        _networkInterface = networkInterface ?? throw new ArgumentNullException(nameof(networkInterface));
        var ipProps = _networkInterface.GetIPProperties();
        var ip4Addresses = ipProps.UnicastAddresses.Where(x => x.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork).Select(x => x.Address);
        Name = $"{_networkInterface.Name} ({string.Join(", ", ip4Addresses.Select(x => x.ToString()))})";
        Enabled = _networkInterface.OperationalStatus == OperationalStatus.Up;
        Addresses = ip4Addresses.ToList();
    }

    public List<IPAddress> Addresses { get; }

    public NetworkInterface NetworkInterface => _networkInterface;

    public string Name
    {
        get => _name;
        set => SetValueAndNotify(ref _name, value);
    }

    public string Id => _networkInterface.Id;

    public bool Forwarded
    {
        get => _forwarded;
        set => SetValueAndNotify(ref _forwarded, value);
    }

    public bool Enabled
    {
        get => _enabled;
        set => SetValueAndNotify(ref _enabled, value);
    }
}

