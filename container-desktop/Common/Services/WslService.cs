namespace ContainerDesktop.Common.Services;

using Microsoft.Win32;


public sealed class WslService : IWslService
{
    private const string RegKeyLxss = "Software\\Microsoft\\Windows\\CurrentVersion\\Lxss";

    private readonly IProcessExecutor _processExecutor;
    private readonly ILogger<WslService> _logger;

    public WslService(IProcessExecutor processExecutor, ILogger<WslService> logger)
    {
        _processExecutor = processExecutor ?? throw new ArgumentNullException(nameof(processExecutor));
        _logger = logger ?? throw new ArgumentNullException(nameof(processExecutor));
    }

    public bool Import(string distributionName, string installLocation, string rootfsFileName)
    {
        var args = new ArgumentBuilder()
            .Add("--import")
            .Add(distributionName)
            .Add(installLocation, true)
            .Add(rootfsFileName, true)
            .Add("--version", "2")
            .Build();
        var ret = _processExecutor.Execute("wsl.exe", args, stdOut: LogStdOut, stdErr: LogStdError);
        return ret == 0;
    }

    public bool Terminate(string distroName)
    {
        var args = new ArgumentBuilder("--terminate")
            .Add(distroName)
            .Build();
        var ret = _processExecutor.Execute("wsl.exe", args, stdOut: LogStdOut, stdErr: LogStdError);
        return ret == 0;
    }

    public bool Unregister(string distroName)
    {
        var args = new ArgumentBuilder("--unregister")
            .Add(distroName)
            .Build();
        var ret = _processExecutor.Execute("wsl.exe", args, stdOut: LogStdOut, stdErr: LogStdError);
        return ret == 0;
    }

    public IEnumerable<string> GetDistros()
    {
        List<string> distros = new();
        using var lxssKey = Registry.CurrentUser.OpenSubKey(RegKeyLxss);
        if (lxssKey != null)
        {
            foreach (var id in lxssKey.GetSubKeyNames())
            {
                using var distroKey = lxssKey.OpenSubKey(id);
                if (distroKey != null)
                {
                    var name = (string)distroKey.GetValue("DistributionName");
                    distros.Add(name);
                }
            }
        }
        else
        {
            var args = new ArgumentBuilder("-l")
                .Add("-q")
                .Build();
            _processExecutor.Execute("wsl.exe", args, stdOut: s => distros.Add(s), stdErr: LogStdError);
        }
        return distros;
    }

    public bool IsInstalled(string distroName)
    {
        var distros = GetDistros();
        return distros.Any(x => x.Equals(distroName, StringComparison.OrdinalIgnoreCase));
    }

    public bool ExecuteCommand(string command, string distroName)
    {
        var args = new ArgumentBuilder()
            .Add("-d", distroName)
            .Add(command)
            .Build();
        var ret = _processExecutor.Execute("wsl.exe", args, stdOut: LogStdOut, stdErr: LogStdError);
        return ret == 0;
    }

    private void LogStdOut(string s)
    {
        _logger.LogInformation(s);
    }

    private void LogStdError(string s)
    {
        _logger.LogError(s);
    }
}
