namespace ContainerDesktop.Common.DesiredStateConfiguration;

using System.IO.Compression;

public class Unpack : ResourceBase
{
    public Uri ResourceUri { get; set; }

    public string TargetDirectory { get; set; }

    public override void Set(ConfigurationContext context)
    {
        var expandedTargetDirectory = Environment.ExpandEnvironmentVariables(TargetDirectory);
        if (context.Uninstall)
        {
            context.FileSystem.Directory.Delete(expandedTargetDirectory, true);
        }
        else
        {
            if (!context.FileSystem.Directory.Exists(expandedTargetDirectory))
            {
                context.FileSystem.Directory.CreateDirectory(expandedTargetDirectory);
            }
            using var s = ResourceUtilities.GetPackContent(ResourceUri);
            using var archive = new ZipArchive(s, ZipArchiveMode.Read, true);
            archive.ExtractToDirectory(expandedTargetDirectory, true);
        }
    }

    public override bool Test(ConfigurationContext context)
    {
        if (context.Uninstall)
        {
            var expandedTargetDirectory = Environment.ExpandEnvironmentVariables(TargetDirectory);
            return !context.FileSystem.Directory.Exists(expandedTargetDirectory);
        }
        else
        {
            //TODO:
            return false;
        }
    }
}
