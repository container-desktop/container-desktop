namespace ContainerDesktop.Common.DesiredStateConfiguration;

public class CreateDirectory : ResourceBase
{
    public string Directory { get; set; }

    public override void Set(ConfigurationContext context)
    {
        var expandedDirectory = Environment.ExpandEnvironmentVariables(Directory);
        if (context.Uninstall)
        {
            context.FileSystem.Directory.Delete(expandedDirectory, true);
        }
        else
        {
            context.FileSystem.Directory.CreateDirectory(expandedDirectory);
        }
    }

    public override bool Test(ConfigurationContext context)
    {
        var expandedDirectory = Environment.ExpandEnvironmentVariables(Directory);
        var exists = context.FileSystem.Directory.Exists(expandedDirectory);
        if (context.Uninstall)
        {
            exists = !exists;
        }
        return exists;
    }
}
