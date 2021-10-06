namespace ContainerDesktop.DesiredStateConfiguration;

using System.IO.Abstractions;
using System.Security.Cryptography;

public class CopyFile : ResourceBase
{
    public string Source { get; set; }
    public string Target { get; set; }

    private string ExpandedSource => Environment.ExpandEnvironmentVariables(Source);
    private string ExpandedTarget => Environment.ExpandEnvironmentVariables(Target);

    public override void Set(ConfigurationContext context)
    {
        context.FileSystem.File.Copy(ExpandedSource, ExpandedTarget, true);
    }

    public override void Unset(ConfigurationContext context)
    {
        context.FileSystem.File.Delete(ExpandedTarget);
    }

    public override bool Test(ConfigurationContext context)
    {
        if (context.FileSystem.File.Exists(ExpandedTarget))
        {
            var sourceHash = ComputeHash(context.FileSystem, ExpandedSource);
            var targetHash = ComputeHash(context.FileSystem, ExpandedTarget);
            return !sourceHash.SequenceEqual(targetHash);
        }
        return false;
    }

    public byte[] ComputeHash(IFileSystem fileSystem, string fileName)
    {
        using var algorithm = SHA1.Create();
        using var fs = fileSystem.File.OpenRead(fileName);
        return algorithm.ComputeHash(fs);
    }
}
