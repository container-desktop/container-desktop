namespace ContainerDesktop.Services;

using ContainerDesktop.Common;
using ContainerDesktop.Common.Services;
using System.Diagnostics;


public sealed class DefaultContainerEngine : IContainerEngine, IDisposable
{
    private readonly IWslService _wslService;
    private readonly IProcessExecutor _processExecutor;
    private Process _proxyProcess;

    public DefaultContainerEngine(IWslService wslService, IProcessExecutor processExecutor)
    {
        _wslService = wslService ?? throw new ArgumentNullException(nameof(wslService));
        _processExecutor = processExecutor ?? throw new ArgumentNullException(nameof(processExecutor));
    }

    public void Start()
    {
        InitializeAndStartDaemon();
        StartProxy();

        //TODO: configure other distros

    }

    public void Dispose()
    {
        StopProxy();
        _proxyProcess?.Dispose();
    }

    private void InitializeAndStartDaemon()
    {
        if (!_wslService.ExecuteCommand("/usr/local/bin/wsl-init.sh", Product.ContainerDesktopDistroName))
        {
            throw new ContainerEngineException("Could not initialize and start the daemon.");
        }
    }

    private void StartProxy()
    {
        //TODO: Monitor process and restart if killed.

        var proxyPath = Path.Combine(AppContext.BaseDirectory, "Resources", "container-desktop-proxy-windows-amd64.exe");
        //TODO: make settings configurable
        var args = new ArgumentBuilder()
            .Add("--listen-address", "npipe:////./pipe/docker_engine")
            .Add("--target-address", "http://localhost:2375")
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
}
