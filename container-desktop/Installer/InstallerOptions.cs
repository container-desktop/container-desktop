using CommandLine;

namespace ContainerDesktop.Installer;

public abstract class InstallerOptions
{
    [Option("auto-start", HelpText = "Inmediately starts the installation at application start.")]
    public bool AutoStart { get; set; }
}
