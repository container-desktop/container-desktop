namespace ContainerDesktop.Common.DesiredStateConfiguration;

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
        if (context.Uninstall)
        {
            context.FileSystem.Directory.Delete(ExpandedTargetDirectory, true);
        }
        else
        {
            if (!context.FileSystem.Directory.Exists(ExpandedTargetDirectory))
            {
                context.FileSystem.Directory.CreateDirectory(ExpandedTargetDirectory);
            }
            using var s = ResourceUtilities.GetPackContent(ResourceUri);
            using var archive = new ZipArchive(s, ZipArchiveMode.Read, true);
            archive.ExtractToDirectory(ExpandedTargetDirectory, true);
        }
    }

    public override bool Test(ConfigurationContext context)
    {
        var expandedVersion = Environment.ExpandEnvironmentVariables(Version);
        if (context.Uninstall)
        {
            var expandedTargetDirectory = Environment.ExpandEnvironmentVariables(TargetDirectory);
            return !context.FileSystem.Directory.Exists(expandedTargetDirectory);
        }
        else if(!string.IsNullOrWhiteSpace(expandedVersion))
        {
            return expandedVersion == context.InstalledVersion;
        }
        return false;
    }
}
