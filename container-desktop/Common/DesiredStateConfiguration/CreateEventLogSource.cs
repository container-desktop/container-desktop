using System.Diagnostics;

namespace ContainerDesktop.Common.DesiredStateConfiguration;

public class CreateEventLogSource : ResourceBase
{
    public string Source { get; set; }

    private string ExpandedSource => Environment.ExpandEnvironmentVariables(Source);

    public override void Set(ConfigurationContext context)
    {
        if(context.Uninstall)
        {
            EventLog.DeleteEventSource(ExpandedSource);
        }
        else
        {
            EventLog.CreateEventSource(ExpandedSource, null);
        }
    }

    public override bool Test(ConfigurationContext context)
    {
        var exists = EventLog.SourceExists(ExpandedSource);
        if(context.Uninstall)
        {
            exists = !exists;
        }
        return exists;
    }
}
