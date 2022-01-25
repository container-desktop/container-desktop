using ContainerDesktop.Common;

namespace ContainerDesktop.DesiredStateConfiguration;

public class AddToAutoStart : ResourceBase
{
    public string ExePath { get; set; }

    private string ExpandedExePath => Environment.ExpandEnvironmentVariables(ExePath);

    public override void Set(ConfigurationContext context)
    {
        AutoStartHelper.EnableAutoStart(ExpandedExePath);
    }

    public override void Unset(ConfigurationContext context)
    {
        AutoStartHelper.DisableAutoStart(ExpandedExePath);
    }

    public override bool Test(ConfigurationContext context)
    {
        return AutoStartHelper.IsAutoStartEnabled(ExpandedExePath);
    }
}
