namespace ContainerDesktop.Installer;

using CommandLine;

[Verb("install", isDefault: true, HelpText = "Installs Container Desktop")]
public class InstallOptions : InstallerOptions
{
}
