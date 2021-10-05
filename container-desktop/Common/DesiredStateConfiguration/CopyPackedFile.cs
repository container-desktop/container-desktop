namespace ContainerDesktop.Common.DesiredStateConfiguration;

public class CopyPackedFile : ResourceBase
{
    public string TargetDirectory { get; set; }

    public Uri ResourceUri { get; set; }

    private string ExpandedTargetDirectory => Environment.ExpandEnvironmentVariables(TargetDirectory);

    private string TargetFilePath => Path.Combine(ExpandedTargetDirectory, Path.GetFileName(ResourceUri.LocalPath));

    public override void Set(ConfigurationContext context)
    {
        using var s = ResourceUtilities.GetPackContent(ResourceUri);
        using var fs = context.FileSystem.File.Create(TargetFilePath);
        s.CopyTo(fs);
    }

    public override void Unset(ConfigurationContext context)
    {
        if (context.FileSystem.File.Exists(TargetFilePath))
        {
            context.FileSystem.File.Delete(TargetFilePath);
        }
    }

    public override bool Test(ConfigurationContext context)
    {
        return context.FileSystem.File.Exists(TargetFilePath);
    }
}
