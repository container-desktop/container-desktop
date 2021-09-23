using System;
using System.IO.Abstractions;
using System.Linq;
using System.Security.Cryptography;

namespace ContainerDesktop.Common.DesiredStateConfiguration
{
    public class CopyFile : ResourceBase
    {
        public string Source { get; set; }
        public string Target { get; set; }

        public override void Set(ConfigurationContext context)
        {
            var expandedTarget = Environment.ExpandEnvironmentVariables(Target);
            var expandedSource = Environment.ExpandEnvironmentVariables(Source);
            if (context.Uninstall)
            {
                context.FileSystem.File.Delete(expandedTarget);
            }
            else
            {
                context.FileSystem.File.Copy(expandedSource, expandedTarget, true);
            }
        }

        public override bool Test(ConfigurationContext context)
        {
            var expandedTarget = Environment.ExpandEnvironmentVariables(Target);
            var expandedSource = Environment.ExpandEnvironmentVariables(Source);
            if (context.Uninstall)
            {
                return context.FileSystem.File.Exists(expandedTarget);
            }
            else if(context.FileSystem.File.Exists(expandedTarget))
            {
                var sourceHash = ComputeHash(context.FileSystem, expandedSource);
                var targetHash = ComputeHash(context.FileSystem, expandedTarget);
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
}
