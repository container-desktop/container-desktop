namespace ContainerDesktop.Common.DesiredStateConfiguration;

using Microsoft.Dism;

public sealed class Wsl : ResourceBase, IDisposable
{
    private const string WslFeatureName = "Microsoft-Windows-Subsystem-Linux";
    private const string VirtualMachinePlatformFeatureName = "VirtualMachinePlatform";
    
    public Wsl()
    {
        DismApi.Initialize(DismLogLevel.LogErrors);
    }

    public void Dispose()
    {
        DismApi.Shutdown();
    }

    public override void Set(ConfigurationContext context)
    {
        using var session = DismApi.OpenOnlineSession();
        
        if (context.Uninstall)
        {
            DismApi.DisableFeature(session, string.Join(';', new[] { WslFeatureName, VirtualMachinePlatformFeatureName }));
        }
        else
        {
            DismApi.EnableFeature(session, string.Join(';', new[] { VirtualMachinePlatformFeatureName, WslFeatureName }));
        }
    }

    public override bool Test(ConfigurationContext context)
    {
        bool enabled = false;
        try
        {
            using var session = DismApi.OpenOnlineSession();
            var info = DismApi.GetFeatureInfo(session, WslFeatureName);
            enabled = info.FeatureState == DismPackageFeatureState.Installed;
        }
        catch
        {
            enabled = false;
        }
        if (context.Uninstall)
        {
            enabled = !enabled;
        }
        return enabled;
    }
}
