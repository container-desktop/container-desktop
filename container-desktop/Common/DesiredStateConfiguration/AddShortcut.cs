namespace ContainerDesktop.Common.DesiredStateConfiguration;

public class AddShortcut : ResourceBase
{
    public Environment.SpecialFolder Location { get; set; }

    public string Name { get; set; }

    public string LinkDescription { get; set; }

    public string TargetPath { get; set; }

    public string LinkFileName => Path.Combine(Environment.GetFolderPath(Location), $"{Environment.ExpandEnvironmentVariables(Name)}.lnk");

    public override void Set(ConfigurationContext context)
    {
        if (context.Uninstall)
        {
            context.FileSystem.File.Delete(LinkFileName);
        }
        else
        {
            var shellLink = new ShellLink();
            shellLink.Description = Environment.ExpandEnvironmentVariables(LinkDescription);
            shellLink.TargetPath = Environment.ExpandEnvironmentVariables(TargetPath);
            shellLink.Save(LinkFileName);
        }
    }

    public override bool Test(ConfigurationContext context)
    {
        var fileExists = context.FileSystem.File.Exists(LinkFileName);
        if (context.Uninstall)
        {
            fileExists = !fileExists;
        }
        return fileExists;
    }
}
