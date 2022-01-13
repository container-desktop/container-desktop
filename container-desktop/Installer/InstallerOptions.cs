using CommandLine;
using CommandLine.Text;

namespace ContainerDesktop.Installer;

public abstract class InstallerOptions
{
    private static readonly Type[] _verbOptions = new[] { typeof(InstallOptions), typeof(UninstallOptions) };

    [Option("auto-start", HelpText = "Inmediately starts the installation at application start.")]
    public bool AutoStart { get; set; }

    [Option("quiet", HelpText = "Do not prompt the user.")]
    public bool Quiet { get; set; }

    [Option("unattended", HelpText = "Do an unattended install, this implicitly implies AutoStart and Quiet.")]
    public bool Unattended { get; set; }

    [Option('s', "settings", HelpText = "Set a configuration setting.")]
    public IEnumerable<string> Settings { get; set; }

    public static InstallerOptions ParseOptions(string[] args)
    {
        var parserResult = Parser.Default.ParseArguments(args, _verbOptions);
        InstallerOptions options = null;
        parserResult.WithParsed<InstallerOptions>(o => options = o)
            .WithNotParsed(e =>
            {
                var message = HelpText.AutoBuild(parserResult);
                throw new CommandLineException(message);                 
            });
        return PostConfigureOptions(options);
    }

    private static InstallerOptions PostConfigureOptions(InstallerOptions options)
    {
        if (options.Quiet)
        {
            options.AutoStart = true;
        }
        if (options.Unattended)
        {
            options.AutoStart = true;
            options.Quiet = true;
        }
        return options;
    }
}
