namespace ContainerDesktop.DesiredStateConfiguration;

public class CreateDirectory : ResourceBase
{
    public string Directory { get; set; }

    private string ExpandedDirectory => Environment.ExpandEnvironmentVariables(Directory);

    public override void Set(ConfigurationContext context)
    {
        context.FileSystem.Directory.CreateDirectory(ExpandedDirectory);
    }

    public override void Unset(ConfigurationContext context)
    {
        context.FileSystem.Directory.Delete(ExpandedDirectory, true);
    }

    public override bool Test(ConfigurationContext context)
    {
        return context.FileSystem.Directory.Exists(ExpandedDirectory);
    }
}
