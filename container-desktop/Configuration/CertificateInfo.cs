using Newtonsoft.Json;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace ContainerDesktop.Configuration;

public class CertificateInfo
{
    public string? Thumbprint { get; set; }
    
    public string? Name { get; set; }
    
    [JsonConverter(typeof(Newtonsoft.Json.Converters.StringEnumConverter))]
    
    public StoreName StoreName { get; set; }
    
    public StoreLocation StoreLocation { get; set; }

    public string? FileName { get; set; }

    public override string ToString() => Name ?? string.Empty;

    public string GetPem()
    {
        using var store = new X509Store(StoreName, StoreLocation, OpenFlags.ReadOnly);
        var cert = store.Certificates.FirstOrDefault(x => x.Thumbprint == Thumbprint);
        if(cert != null)
        {
            return new string(PemEncoding.Write("CERTIFICATE", cert.RawData));
        }
        return string.Empty;
    }

    public override bool Equals(object? obj)
    {
        if(obj is CertificateInfo other)
        {
            return Thumbprint == other.Thumbprint;
        }
        return false;
    }

    public override int GetHashCode()
    {
        return Thumbprint?.GetHashCode() ?? 0;
    }

    public static CertificateInfo FromCertificate(X509Certificate2 cert, StoreName storeName, StoreLocation storeLocation)
    {
        cert = cert ?? throw new ArgumentNullException(nameof(cert));
        return new CertificateInfo
        {
            Thumbprint = cert.Thumbprint,
            Name = cert.GetNameInfo(X509NameType.SimpleName, false),
            StoreLocation = storeLocation,
            StoreName = storeName,
            FileName = $"cd-{cert.Thumbprint}.crt"
        };
    }

    public static IEnumerable<CertificateInfo> GetCertificates()
    {
        using var machineRootStore = new X509Store(StoreName.Root, StoreLocation.LocalMachine, OpenFlags.ReadOnly);
        using var machineIntermediateStore = new X509Store(StoreName.CertificateAuthority, StoreLocation.LocalMachine, OpenFlags.ReadOnly);
        using var userRootStore = new X509Store(StoreName.Root, StoreLocation.CurrentUser, OpenFlags.ReadOnly);
        using var userIntermediateStore = new X509Store(StoreName.CertificateAuthority, StoreLocation.CurrentUser, OpenFlags.ReadOnly);

        return machineRootStore.Certificates.Where(x => x.Verify()).Select(x => new { StoreName = StoreName.Root, StoreLocation = StoreLocation.LocalMachine, Certificate = x })
            .Union(machineIntermediateStore.Certificates.Where(x => x.Verify()).Select(x => new { StoreName = StoreName.Root, StoreLocation = StoreLocation.LocalMachine, Certificate = x }))
            .Union(userRootStore.Certificates.Where(x => x.Verify()).Select(x => new { StoreName = StoreName.Root, StoreLocation = StoreLocation.LocalMachine, Certificate = x }))
            .Union(userIntermediateStore.Certificates.Where(x => x.Verify()).Select(x => new { StoreName = StoreName.Root, StoreLocation = StoreLocation.LocalMachine, Certificate = x }))
            .Select(x => FromCertificate(x.Certificate, x.StoreName, x.StoreLocation))
            .OrderBy(x => x.Name);
    }
}
