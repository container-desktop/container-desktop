using Microsoft.Dism;

namespace ContainerDesktop.Common.DesiredStateConfiguration;

public sealed class EnableWindowsFeatures : ResourceBase, IDisposable
{
    public List<string> Features { get; } = new List<string>();

    public bool IgnoreRebootRequired { get; set; }

    public bool All { get; set; } = true;

    public EnableWindowsFeatures()
    {
        DismApi.Initialize(DismLogLevel.LogErrors);
    }

    public void Dispose()
    {
        DismApi.Shutdown();
    }

    public override void Set(ConfigurationContext context)
    {
        if(Features.Count == 0)
        {
            context.Logger.LogWarning("[{ResourceId}]: No features specified.", Id);
            return;
        }

        using var session = DismApi.OpenOnlineSession();
        var enabledFeatures = GetEnabledFeatures(session);
        context.Logger.LogInformation("Enabled features: {Features}", string.Join(';', enabledFeatures));
        try
        {
            if (context.Uninstall)
            {       
                var features = string.Join(';', enabledFeatures);
                if (features.Length > 0)
                {
                    DismApi.DisableFeature(session, features, null, false);
                }
            }
            else
            {
                var features = string.Join(';', Features.Except(enabledFeatures));
                if (features.Length > 0)
                {
                    DismApi.EnableFeature(session, features, false, All);
                }
            }
        }
        catch(DismRebootRequiredException)
        {
            context.Logger.LogInformation("Reboot required: RequiresReboot={RequiresReboot}, IgnoreRebootRequires={IgnoreRebootRequires}", RequiresReboot, IgnoreRebootRequired);
            RequiresReboot = RequiresReboot || !IgnoreRebootRequired;
        }
    }

    public override bool Test(ConfigurationContext context)
    {
        bool enabled = false;
        try
        {
            using var session = DismApi.OpenOnlineSession();
            var enabledFeatures = GetEnabledFeatures(session);
            enabled = enabledFeatures.Count == Features.Count;
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

    private List<string> GetEnabledFeatures(DismSession session)
    {
        return Features
            .Where(x => DismApi.GetFeatureInfo(session, x).FeatureState == DismPackageFeatureState.Installed)
            .ToList();
    }
}

