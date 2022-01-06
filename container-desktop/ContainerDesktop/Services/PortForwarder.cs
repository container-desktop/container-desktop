using ContainerDesktop.Processes;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;

namespace ContainerDesktop.Services;

public class PortForwarder
{
    private readonly Socket _mainSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
    private readonly CancellationTokenSource _cts = new CancellationTokenSource();
    private List<Task> _runningTasks = new List<Task>();
    private readonly IProcessExecutor _processExecutor;
    private Process _forwarder;

    public PortForwarder(IProcessExecutor processExecutor)
    {
        _processExecutor = processExecutor ?? throw new ArgumentNullException(nameof(processExecutor));
    }

    public void Start(IPEndPoint local, IPEndPoint remote)
    {
        var proxyPath = Path.Combine(AppContext.BaseDirectory, "Resources", "container-desktop-port-forwarder.exe");
        var args = new ArgumentBuilder()
            .Add("-proto", "tcp")
            .Add("-frontend-ip", local.Address.ToString())
            .Add("-frontend-port", local.Port.ToString())
            .Add("-backend-ip", remote.Address.ToString())
            .Add("-backend-port", remote.Port.ToString())
            .Build();
        _forwarder = _processExecutor.Start(proxyPath, args);
        if (_forwarder.WaitForExit(1000))
        {
            throw new ContainerEngineException("Failed to start the proxy.");
        }
    }

    public void Stop()
    {
        if (_forwarder?.HasExited == false)
        {
            _forwarder.Kill();
        }
    }
}
