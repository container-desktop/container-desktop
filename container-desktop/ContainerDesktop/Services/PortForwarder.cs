using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace ContainerDesktop.Services;

public class PortForwarder
{
    private readonly Socket _mainSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
    private readonly CancellationTokenSource _cts = new CancellationTokenSource();
    private List<Task> _runningTasks = new List<Task>();

    public void Start(IPEndPoint local, IPEndPoint remote)
    {
        _mainSocket.Bind(local);
        _mainSocket.Listen(10);

        _runningTasks.Add(Task.Run(async () =>
        {
            while (!_cts.IsCancellationRequested)
            {
                var source = await _mainSocket.AcceptAsync(_cts.Token);
                var destination = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                await destination.ConnectAsync(remote, _cts.Token);
                var sourceStream = new NetworkStream(source);
                var destinationStream = new NetworkStream(destination);
                var copySourceToDestination = Task.Run(() => sourceStream.CopyToAsync(destinationStream, _cts.Token));
                var copyDestinationToSource = Task.Run(() => destinationStream.CopyToAsync(sourceStream, _cts.Token));
                _runningTasks.Add(Task.WhenAll(copySourceToDestination, copyDestinationToSource));
            }
        }));
    }

    public void Stop()
    {
        _cts.Cancel();
    }
}
