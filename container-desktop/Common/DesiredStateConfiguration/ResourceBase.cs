namespace ContainerDesktop.Common.DesiredStateConfiguration;

public abstract class ResourceBase : IResource
{
    public string Id { get; set; }

    public List<string> DependsOn { get; } = new List<string>();

    public bool Enabled { get; set; } = true;

    public string Description { get; set; }

    public bool NoUninstall { get; set; }
    public bool RequiresReboot { get; set; }
    public bool RunAllwaysFirst { get; set; }

    public abstract void Set(ConfigurationContext context);

    public abstract bool Test(ConfigurationContext context);
}
