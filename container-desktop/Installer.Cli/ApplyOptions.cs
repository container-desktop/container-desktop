using CommandLine;

namespace Installer.Cli
{
    [Verb("apply", isDefault: true, HelpText = "Applies a desired state configuration file.")]
    public class ApplyOptions
    {
        [Option('c', "configuration-manifest", Required = true, HelpText = "The configuration manifest to apply")]
        public string ConfigurationManifestFileName { get; set; }
    }
}
