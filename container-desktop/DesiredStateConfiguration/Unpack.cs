namespace ContainerDesktop.DesiredStateConfiguration;

using System.IO.Compression;

public class Unpack : ResourceBase
{
    public Uri ResourceUri { get; set; }

    public string TargetDirectory { get; set; }

    public string VersionFilePath { get; set; }

    public string Version { get; set; }

    private string ExpandedTargetDirectory => Environment.ExpandEnvironmentVariables(TargetDirectory);

    public override void Set(ConfigurationContext context)
    {
        if (!context.FileSystem.Directory.Exists(ExpandedTargetDirectory))
        {
            context.FileSystem.Directory.CreateDirectory(ExpandedTargetDirectory);
        }
        using var s = ResourceUtilities.GetPackContent(ResourceUri);
        using var archive = new ZipArchive(s, ZipArchiveMode.Read, true);
        archive.ExtractToDirectory(ExpandedTargetDirectory, true);
    }

    public override void Unset(ConfigurationContext context)
    {
        context.FileSystem.Directory.Delete(ExpandedTargetDirectory, true);
    }

    public override bool Test(ConfigurationContext context)
    {
        return context.FileSystem.Directory.Exists(ExpandedTargetDirectory);
    }
}
