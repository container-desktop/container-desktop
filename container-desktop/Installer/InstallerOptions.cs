using CommandLine;

namespace ContainerDesktop.Installer;

public abstract class InstallerOptions
{
    [Option("auto-start", HelpText = "Inmediately starts the installation at application start.")]
    public bool AutoStart { get; set; }

    [Option("quiet", HelpText = "Do not prompt the user.")]
    public bool Quiet { get; set; }

    [Option("unattended", HelpText = "Do an unattended install, this implicitly implies AutoStart and Quiet.")]
    public bool Unattended { get; set; }
}
