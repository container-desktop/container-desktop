using System.Net.NetworkInformation;

namespace ContainerDesktop.Configuration;

public class AdapterInfo
{
    public string? Id { get; set; }
    public string? Name { get; set; }

    public static IEnumerable<AdapterInfo> GetAdapters()
    {
        return NetworkInterface.GetAllNetworkInterfaces()
            .Where(x => x.OperationalStatus == OperationalStatus.Up && x.NetworkInterfaceType != NetworkInterfaceType.Loopback)
            .Select(x => new AdapterInfo { Id = x.Id, Name = x.Name });
    }

    public override bool Equals(object? obj)
    {
        if (obj is AdapterInfo other)
        {
            return Id == other.Id;
        }
        return false;
    }

    public override int GetHashCode()
    {
        return Id?.GetHashCode() ?? 0;
    }

    public override string ToString() => Name ?? string.Empty;
}
