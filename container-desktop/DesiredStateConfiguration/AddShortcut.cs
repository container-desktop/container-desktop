namespace ContainerDesktop.DesiredStateConfiguration;

public class AddShortcut : ResourceBase
{
    public Environment.SpecialFolder Location { get; set; }

    public string Name { get; set; }

    public string LinkDescription { get; set; }

    public string TargetPath { get; set; }

    public string LinkFileName => Path.Combine(Environment.GetFolderPath(Location), $"{Environment.ExpandEnvironmentVariables(Name)}.lnk");

    public override void Set(ConfigurationContext context)
    {
        ShellLink shellLink = new() 
        {
            Description = Environment.ExpandEnvironmentVariables(LinkDescription),
            TargetPath = Environment.ExpandEnvironmentVariables(TargetPath)
        };
        shellLink.Save(LinkFileName);
    }

    public override void Unset(ConfigurationContext context)
    {
        context.FileSystem.File.Delete(LinkFileName);
    }

    public override bool Test(ConfigurationContext context)
    {
        return context.FileSystem.File.Exists(LinkFileName);
    }
}
