using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Management.Automation;

namespace ContainerDesktop.Common.Services
{
    public sealed class WslService : IWslService
    {
        private const string WslFeatureName = "Microsoft-Windows-Subsystem-Linux";
        private const string VMPFeatureName = "VirtualMachinePlatform";
        private const string RegKeyLxss = "Software\\Microsoft\\Windows\\CurrentVersion\\Lxss";

        private readonly IProcessExecutor _processExecutor;
        private readonly ILogger<WslService> _logger;

        public WslService(IProcessExecutor processExecutor, ILogger<WslService> logger)
        {
            _processExecutor = processExecutor ?? throw new ArgumentNullException(nameof(processExecutor));
            _logger = logger ?? throw new ArgumentNullException(nameof(processExecutor));
        }

        public string ContainerDesktopDistroName { get; } = "container-desktop";

        public bool IsEnabled()
        {
            try
            {
                var ret = _processExecutor.Execute("wsl", "--status");
                return ret == 0;
            }
            catch
            {
                return false;
            }
        }

        public bool Enable()
        {
            return EnableFeature(VMPFeatureName) &&
                   EnableFeature(WslFeatureName);
        }

        public bool Import(string installLocation, string rootfsFileName)
        {
            return Import(ContainerDesktopDistroName, installLocation, rootfsFileName);
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
            var ret  = _processExecutor.Execute("wsl.exe", args, stdOut: LogStdOut, stdErr: LogStdError);
            return ret == 0;
        }

        public bool Terminate()
        {
            var args = new ArgumentBuilder("--terminate")
                .Add(ContainerDesktopDistroName)
                .Build();
            var ret = _processExecutor.Execute("wsl.exe", args, stdOut: LogStdOut, stdErr: LogStdError);
            return ret == 0;
        }

        public bool Unregister()
        {
            var args = new ArgumentBuilder("--unregister")
                .Add(ContainerDesktopDistroName)
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

        public bool IsInstalled() => IsInstalled(ContainerDesktopDistroName);

        public bool IsInstalled(string distroName)
        {
            var distros = GetDistros();
            return distros.Any(x => x.Equals(distroName, StringComparison.OrdinalIgnoreCase));
        }

        public bool ExecuteCommand(string command) => ExecuteCommand(command, ContainerDesktopDistroName);
        
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

        private bool EnableFeature(string featureName)
        {
            var script = $"Get-WindowsOptionalFeature -Online -FeatureName '{featureName}' | Where-Object State -ne Enabled |  Foreach-Object {{ Enable-WindowsOptionalFeature -Online -FeatureName $_.FeatureName -All -NoRestart }};";
            (_, var ret) = InvokeScript(script);
            return ret;
        }

        private (Collection<PSObject>, bool) InvokeScript(string script)
        {
            using var ps = PowerShell.Create();
            using var _ = ps.Streams.WriteToLog(_logger);
            script = $"Set-ExecutionPolicy -ExecutionPolicy Bypass; Import-Module Dism; {script}";
            ps.AddScript(script);
            return (ps.Invoke(), ps.HadErrors);
        }
    }
}
