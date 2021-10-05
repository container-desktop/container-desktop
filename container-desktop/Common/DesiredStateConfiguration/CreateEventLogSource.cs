using System.Diagnostics;

namespace ContainerDesktop.Common.DesiredStateConfiguration;

public class CreateEventLogSource : ResourceBase
{
    public string Source { get; set; }

    private string ExpandedSource => Environment.ExpandEnvironmentVariables(Source);

    public override void Set(ConfigurationContext context)
    {
        EventLog.CreateEventSource(ExpandedSource, null);
    }

    public override void Unset(ConfigurationContext context)
    {
        EventLog.DeleteEventSource(ExpandedSource);
    }

    public override bool Test(ConfigurationContext context)
    {
        return EventLog.SourceExists(ExpandedSource);
    }
}
