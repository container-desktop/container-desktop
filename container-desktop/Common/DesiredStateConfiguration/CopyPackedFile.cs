namespace ContainerDesktop.Common.DesiredStateConfiguration;

public class CopyPackedFile : ResourceBase
{
    public string TargetDirectory { get; set; }

    public Uri ResourceUri { get; set; }

    private string ExpandedTargetDirectory => Environment.ExpandEnvironmentVariables(TargetDirectory);

    private string TargetFilePath => Path.Combine(ExpandedTargetDirectory, Path.GetFileName(ResourceUri.LocalPath));

    public override void Set(ConfigurationContext context)
    {
        if(context.Uninstall)
        {
            if(context.FileSystem.File.Exists(TargetFilePath))
            {
                context.FileSystem.File.Delete(TargetFilePath);
            }
        }
        else
        {
            using var s = ResourceUtilities.GetPackContent(ResourceUri);
            using var fs = context.FileSystem.File.Create(TargetFilePath);
            s.CopyTo(fs);
        }
    }

    public override bool Test(ConfigurationContext context)
    {
        var exists = context.FileSystem.File.Exists(TargetFilePath);
        if(context.Uninstall)
        {
            exists = !exists;
        }
        return exists;
    }
}
