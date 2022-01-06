using System.Net;
using System.Net.Sockets;

namespace ContainerDesktop.Services;

internal class PortForwarder2
{
    private readonly Socket _mainSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
    private readonly CancellationTokenSource _cts = new CancellationTokenSource();
    private List<Task> _runningTasks = new List<Task>();
    private readonly ILogger _logger;

    public PortForwarder2(ILogger logger)
    {
        _logger = logger;
    }

    public void Start(IPEndPoint local, IPEndPoint remote)
    {
        _mainSocket.Bind(local);
        _mainSocket.Listen();

        _runningTasks.Add(Task.Run(async () =>
        {
            try
            {
                while (!_cts.IsCancellationRequested)
                {
                    var source = await _mainSocket.AcceptAsync(_cts.Token);
                    var destination = new PortForwarder2(_logger);
                    var state = new State(source, destination._mainSocket);
                    destination.Connect(remote, source);
                    source.BeginReceive(state.Buffer, 0, state.Buffer.Length, 0, OnDataReceive, state);
                }
            }
            catch(TaskCanceledException)
            {
                return;
            }
            catch
            {
                throw;
            }
        }));
    }

    public void Stop()
    {
        try
        {
            _cts.Cancel();
            Task.WaitAll(_runningTasks.ToArray());
        }
        catch (AggregateException ex) when (ex.InnerExceptions.All(x => x is TaskCanceledException))
        {
        }
        finally
        {
            try
            {
                _mainSocket.Close();
                _mainSocket.Dispose();
            }
            catch (Exception ex)
            {
            }
        }
    }

    private void Connect(EndPoint remoteEndpoint, Socket destination)
    {
        var state = new State(_mainSocket, destination);
        _mainSocket.Connect(remoteEndpoint);
        _mainSocket.BeginReceive(state.Buffer, 0, state.Buffer.Length, SocketFlags.None, OnDataReceive, state);
    }

    private void OnDataReceive(IAsyncResult result)
    {
        var state = (State)result.AsyncState;
        try
        {
            var bytesRead = state.SourceSocket.EndReceive(result);
            if (bytesRead > 0)
            {
                state.DestinationSocket.Send(state.Buffer, bytesRead, SocketFlags.None);
                state.SourceSocket.BeginReceive(state.Buffer, 0, state.Buffer.Length, 0, OnDataReceive, state);
            }
        }
        catch(Exception ex)
        {
            _logger.LogError(ex, ex.Message);
            state.DestinationSocket.Close();
            state.SourceSocket.Close();
        }
    }

    private class State
    {
        public Socket SourceSocket { get; private set; }
        public Socket DestinationSocket { get; private set; }
        public byte[] Buffer { get; private set; }

        public State(Socket source, Socket destination)
        {
            SourceSocket = source;
            DestinationSocket = destination;
            Buffer = new byte[8192];
        }
    }
}
