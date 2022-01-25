namespace ContainerDesktop.Wsl;

using ContainerDesktop.Processes;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using System.Threading;

#pragma warning disable CA2254

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

    public bool ExecuteCommand(string command, string distroName, string user = null, Action<string> stdout = null, Action<string> stderr = null)
    {
        var args = new ArgumentBuilder()
            .Add("-d", distroName)
            .AddIf("--user", user, !string.IsNullOrWhiteSpace(user))
            .Add(command)
            .Build();
        var ret = _processExecutor.Execute("wsl.exe", args, stdOut: stdout ?? LogStdOut, stdErr: stderr ?? LogStdError);
        return ret == 0;
    }

    public async Task<bool> ExecuteCommandAsync(string command, string distroName, string user = null, Action<string> stdout = null, Action<string> stderr = null, CancellationToken cancellationToken = default)
    {
        var args = new ArgumentBuilder()
            .Add("-d", distroName)
            .AddIf("--user", user, !string.IsNullOrWhiteSpace(user))
            .Add(command)
            .Build();
        var ret = await _processExecutor.ExecuteAsync("wsl.exe", args, stdOut: stdout ?? LogStdOut, stdErr: stderr ?? LogStdError, cancellationToken: cancellationToken);
        return ret == 0;
    }

    private void LogStdOut(string s)
    {
        _logger.LogInformation(s);
    }

    private void LogStdError(string s)
    {
        _logger.LogInformation(s);
    }
}

#pragma warning restore CA2254
