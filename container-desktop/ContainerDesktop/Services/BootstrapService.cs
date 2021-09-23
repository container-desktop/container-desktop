using ContainerDesktop.Common;
using ContainerDesktop.Common.Services;
using System;
using System.Diagnostics;
using System.IO;

namespace ContainerDesktop.Services
{
    public sealed class BootstrapService : IBootstrapService, IDisposable
    {
        private readonly IWslService _wslService;
        private readonly IProcessExecutor _processExecutor;
        private Process _proxyProcess;

        public BootstrapService(IWslService wslService, IProcessExecutor processExecutor)
        {
            _wslService = wslService ?? throw new ArgumentNullException(nameof(wslService));
            _processExecutor = processExecutor ?? throw new ArgumentNullException(nameof(processExecutor));
        }

        public void Bootstrap()
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
            if(!_wslService.ExecuteCommand("/usr/local/bin/wsl-init.sh"))
            {
                throw new BootstrapException("Could not initialize and start the daemon.");
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
        }

        private void StopProxy()
        {
            if (_proxyProcess?.HasExited == false)
            {
                _proxyProcess.Kill();
            }
        }
    }
}
