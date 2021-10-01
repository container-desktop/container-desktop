namespace ContainerDesktop.Services;

using ContainerDesktop.Common;
using ContainerDesktop.Common.Services;
using Docker.DotNet;
using System.Diagnostics;
using System.Threading;

public sealed class DefaultContainerEngine : IContainerEngine, IDisposable
{
    private readonly IWslService _wslService;
    private readonly IProcessExecutor _processExecutor;
    private readonly IConfigurationService _configurationService;
    private Process _proxyProcess;
    private RunningState _runningState;
    private readonly Dictionary<string, (Task task, CancellationTokenSource cts)> _enabledDistroProxies = new();

    public DefaultContainerEngine(IWslService wslService, IProcessExecutor processExecutor, IConfigurationService configurationService)
    {
        _wslService = wslService ?? throw new ArgumentNullException(nameof(wslService));
        _processExecutor = processExecutor ?? throw new ArgumentNullException(nameof(processExecutor));
        _configurationService = configurationService ?? throw new ArgumentNullException(nameof(configurationService));
        // TODO: query the state
        _runningState = RunningState.Stopped;
    }

    public event EventHandler RunningStateChanged;

    public RunningState RunningState 
    { 
        get => _runningState;
        private set 
        {
            if (_runningState != value)
            {
                _runningState = value;
                if (RunningStateChanged != null)
                {
                    RunningStateChanged(this, EventArgs.Empty);
                }
            }
        } 
    }

    public void Start()
    {
        RunningState = RunningState.Starting;
        _wslService.Terminate(Product.ContainerDesktopDistroName);
        InitializeDataDistro();
        InitializeAndStartDaemon();
        StartProxy();
        WarmupDaemon();
        InitializeDistros();
        RunningState = RunningState.Started;
    }

    private void InitializeDataDistro()
    {
        if (!_wslService.ExecuteCommand($"/wsl-init-data.sh", Product.ContainerDesktopDataDistroName))
        {
            throw new ContainerEngineException("Could not initialize the data distribution.");
        }
    }

    public void Stop()
    {
        RunningState = RunningState.Stopping;
        StopDistros();
        StopProxy();
        _wslService.Terminate(Product.ContainerDesktopDistroName);
        RunningState = RunningState.Stopped;
    }

    public void Restart()
    {
        Stop();
        Start();
    }

    public void EnableDistro(string name, bool enabled)
    {
        if (enabled)
        {
            var cts = new CancellationTokenSource();
            var task = Task.Run(() => _wslService.ExecuteCommandAsync($"/mnt/wsl/container-desktop/distro/wsl-distro-init.sh \"{name}\"", name, "root", cts.Token));
            
            _enabledDistroProxies[name] = (task, cts);
        }
        else
        {
            if(_enabledDistroProxies.TryGetValue(name, out var proxy))
            {
                proxy.cts.Cancel();
                _enabledDistroProxies.Remove(name);
            }
            if (!_wslService.ExecuteCommand($"/mnt/wsl/container-desktop/distro/wsl-distro-rm.sh \"{name}\"", name, "root"))
            {
                throw new ContainerEngineException($"Could not disable the distribution {name}");
            }
        }
    }

    public void Dispose()
    {
        StopProxy();
        _proxyProcess?.Dispose();
        foreach(var proxy in _enabledDistroProxies)
        {
            proxy.Value.cts.Cancel();
        }
    }

    private void InitializeAndStartDaemon()
    {
        var certPath = GetWslPathInDistro(LocalCertsPath);
        if (!_wslService.ExecuteCommand($"/usr/local/bin/wsl-init.sh \"{certPath}\"", Product.ContainerDesktopDistroName))
        {
            throw new ContainerEngineException("Could not initialize and start the daemon.");
        }
    }

    private void InitializeDistros()
    {
        foreach (var distroName in _configurationService.Configuration.EnabledDistributions)
        {
            EnableDistro(distroName, true);
        }
    }

    private void StopDistros()
    {
        foreach (var distroName in _configurationService.Configuration.EnabledDistributions)
        {
            EnableDistro(distroName, false);
        }
    }


    private string LocalCertsPath { get; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), Product.Name, "certs\\");
    
    private string GetWslPathInDistro(string windowsPath)
    {
        var fullPath = Path.GetFullPath(windowsPath);
        var root = Path.GetPathRoot(fullPath);
        var dir = Path.GetDirectoryName(fullPath);
        dir = Path.GetRelativePath(root, dir);
        root = root[..^2].ToLowerInvariant();
        var fileName = Path.GetFileName(fullPath);
        return $"/mnt/host/{root}/{dir}/{fileName}".Replace('\\', '/');
    }

    private void StartProxy()
    {
        //TODO: Monitor process and restart if killed.

        var proxyPath = Path.Combine(AppContext.BaseDirectory, "Resources", "container-desktop-proxy-windows-amd64.exe");
        var processName = Path.GetFileNameWithoutExtension(proxyPath);
        using var existingProcess = Process.GetProcessesByName(processName).FirstOrDefault();
        if(existingProcess != null && !existingProcess.HasExited)
        {
            existingProcess.Kill();
        }

        //TODO: make settings configurable
        var args = new ArgumentBuilder()
            .Add("--listen-address", "npipe:////./pipe/docker_engine")
            .Add("--target-address", "https://localhost:2376")
            .Add("--tls-key", Path.Combine(LocalCertsPath, "key.pem"))
            .Add("--tls-cert", Path.Combine(LocalCertsPath, "cert.pem"))
            .Add("--tls-ca", Path.Combine(LocalCertsPath, "ca.pem"))
            .Build();
        _proxyProcess = _processExecutor.Start(proxyPath, args);
        // Give it some time to startup
        if (_proxyProcess.WaitForExit(1000))
        {
            throw new ContainerEngineException("Failed to start the proxy.");
        }
    }

    private void StopProxy()
    {
        if (_proxyProcess?.HasExited == false)
        {
            _proxyProcess.Kill();
        }
    }

    private void WarmupDaemon()
    {
        //TODO: make client available in DI with all configuration set.
        Task.Run(() =>
        {
            using var client = new DockerClientConfiguration().CreateClient();
            client.Containers.ListContainersAsync(new Docker.DotNet.Models.ContainersListParameters());
        });
    }
}
