namespace ContainerDesktop.Services;

using ContainerDesktop.Common;
using ContainerDesktop.Configuration;
using ContainerDesktop.Processes;
using ContainerDesktop.Wsl;
using Docker.DotNet;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Polly;
using System.Diagnostics;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using System.Threading;

#pragma warning disable CA2254

public sealed class DefaultContainerEngine : IContainerEngine, IDisposable
{
    private readonly IWslService _wslService;
    private readonly IProcessExecutor _processExecutor;
    private readonly IConfigurationService _configurationService;
    private readonly IProductInformation _productInformation;
    private readonly ILogger<DefaultContainerEngine> _logger;
    private readonly Dictionary<string, (Task task, CancellationTokenSource cts)> _enabledDistroProxies = new();
    private readonly Dictionary<string, PortForwarder> _portForwarders = new();
    private readonly List<PortAndProtocol> _ports = new();
    private Process _proxyProcess;
    private RunningState _runningState;
    private Task _dataDistroInitTask;
    private Task _portForwardListenerTask;
    private CancellationTokenSource _cts;
    private CancellationTokenSource _portforwardListenerCts;
    private DnsConfigurator _dnsConfigurator;

    public DefaultContainerEngine(
        IWslService wslService, 
        IProcessExecutor processExecutor, 
        IConfigurationService configurationService, 
        IProductInformation productInformation,
        ILogger<DefaultContainerEngine> logger)
    {
        _wslService = wslService ?? throw new ArgumentNullException(nameof(wslService));
        _processExecutor = processExecutor ?? throw new ArgumentNullException(nameof(processExecutor));
        _configurationService = configurationService ?? throw new ArgumentNullException(nameof(configurationService));
        _productInformation = productInformation ?? throw new ArgumentNullException(nameof(productInformation));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        // TODO: query the state
        _runningState = RunningState.Stopped;
        LocalCertsPath = Path.Combine(_productInformation.ContainerDesktopAppDataDir, "certs\\");
        ConfigurationChangedEventManager.AddHandler(_configurationService, OnConfigurationChanged);
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
                RunningStateChanged?.Invoke(this, EventArgs.Empty);
            }
        } 
    }

    public void Start()
    {
        var retryPolicy = Policy.Handle<Exception>().WaitAndRetry(3, _ => TimeSpan.FromSeconds(2));
        retryPolicy.Execute(() =>
        {
            try
            {
                _cts = new CancellationTokenSource();
                RunningState = RunningState.Starting;
                _wslService.Terminate(_productInformation.ContainerDesktopDistroName);
                InitializeDnsConfigurator();
                InitializeDataDistro();
                InitializePortForwardListener();
                UpdateDaemonConfig();
                UpdateCertificates();
                InitializeAndStartDaemon();
                StartProxy();
                WarmupDaemon();
                InitializeDistros();
                RunningState = RunningState.Running;
            }
            catch (Exception ex)
            {
                try
                {
                    Stop();
                }
                catch (Exception stopEx)
                {
                    var msg = $"{ex.Message}\r\n{stopEx.Message}";
                    throw new AggregateException(msg, ex, stopEx);
                }
                throw;
            }
        });
    }

    private void UpdateCertificates()
    {
        var existingCerts = new List<string>();
        if(!_wslService.ExecuteCommand("ls -1 /usr/local/share/ca-certificates/cd-*.crt", _productInformation.ContainerDesktopDistroName, stdout: s => existingCerts.Add(Path.GetFileName(s))))
        {
            existingCerts.Clear();
        }
        var addedCerts = _configurationService.Configuration.Certificates.Where(x => !existingCerts.Contains(x.FileName));
        var removedCerts = existingCerts.Where(x => !_configurationService.Configuration.Certificates.Any(y => y.FileName == x));
        foreach(var removedCert in removedCerts)
        {
            var path = $"/usr/local/share/ca-certificates/{removedCert}";
            if(_wslService.ExecuteCommand($"rm {path}", _productInformation.ContainerDesktopDistroName))
            {
                _logger.LogInformation($"Removed certificate '{path}'");
            }
            else
            {
                _logger.LogError($"Could not remove certificate '{path}'");
            }
        }
        foreach(var addedCert in addedCerts)
        {
            var path = $"/usr/local/share/ca-certificates/{addedCert.FileName}";
            var pem = addedCert.GetPem();
            if (_wslService.ExecuteCommand($"cat <<EOF > {path}\n{pem}\nEOF\n", _productInformation.ContainerDesktopDistroName))
            {
                _logger.LogInformation($"Added certificate '{path}'");
            }
            else
            {
                _logger.LogError($"Could not add certificate '{path}'");
            }
        }
        var output = new StringBuilder();
        if(_wslService.ExecuteCommand("update-ca-certificates", _productInformation.ContainerDesktopDistroName, stdout: s => output.AppendLine(s)))
        {
            _logger.LogInformation("Updated ca certificates");
        }
        else
        {
            _logger.LogError("Failed to update ca certificates: {output}", output.ToString());
        }
    }

    private void UpdateDaemonConfig()
    {
        _logger.LogInformation("Updating daemon configuration.");
        var json = _configurationService.Configuration.DaemonConfig.Replace("\r\n", "\n");
        if (_wslService.ExecuteCommand($"cat <<EOF > /etc/docker/daemon.json\n{json}\nEOF\n", _productInformation.ContainerDesktopDistroName))
        {
            _logger.LogInformation("Daemon configuration updated.");
        }
        else
        {
            _logger.LogError("Daemon configuration not updated.");
        }
    }

    private void InitializePortForwardListener()
    {
        if (_configurationService.Configuration.PortForwardingEnabled)
        {
            _portforwardListenerCts = CancellationTokenSource.CreateLinkedTokenSource(_cts.Token);
            if(!_wslService.ExecuteCommand("cp /usr/local/bin/docker-proxy-shim /usr/local/bin/docker-proxy", _productInformation.ContainerDesktopDistroName, stdout: s => _logger.LogInformation(s)))
            {
                _logger.LogError("Could not initialize port forwarding.");
                return;
            }
            _portForwardListenerTask = Task.Run(async () =>
            {
                try
                {
                    while (!_portforwardListenerCts.IsCancellationRequested)
                    {
                        _logger.LogInformation("Listening for port forward messages.");
                        await _wslService.ExecuteCommandAsync(
                            "nc -lk -U /var/run/cd-port-forward.sock",
                            _productInformation.ContainerDesktopDistroName,
                            stdout: ForwardPort,
                            cancellationToken: _portforwardListenerCts.Token);
                        if (!_portforwardListenerCts.IsCancellationRequested)
                        {
                            _logger.LogWarning("Listening for port forward messages stopped unexpectedly, trying to restart.");
                            await Task.Delay(100);
                        }
                    }
                    _logger.LogInformation("Stopped listening for port forward messages.");
                }
                catch(TaskCanceledException)
                {
                    _logger.LogInformation("Stopped listening for port forward messages.");
                }
            });
        }
        else
        {
            _portForwardListenerTask = Task.CompletedTask;
        }
    }

    private static readonly Regex _hostipParser = new(@"-host-ip (?'h'::|0\.0\.0\.0)", RegexOptions.Singleline | RegexOptions.Compiled);
    private static readonly Regex _hostPortParser = new(@"-host-port (?'p'\d+)", RegexOptions.Singleline | RegexOptions.Compiled);
    private static readonly Regex _protoParser = new(@"-proto (?'p'(tcp|udp))", RegexOptions.Singleline | RegexOptions.Compiled);

    public sealed record PortAndProtocol(int Port, string Protocol);

    private void ForwardPort(string line)
    {
        try
        {
            var cmdLine = line[2..];
            var enabled = line.StartsWith('O');
            (var ipAddress, var portAndProto) = ParsePortForwardCmdLine(cmdLine);
            _logger.LogInformation("Received port forward command. Enabled={Enabled}, IP Address={IPAddress}, Port={Port}, Protocol={Protocol}", enabled, ipAddress, portAndProto.Port, portAndProto.Protocol);
            if (ipAddress == IPAddress.Any)
            {
                _logger.LogInformation("Processing port forward command for IPv4");
                if (enabled)
                {
                    _logger.LogInformation("Start port forward for port={Port} protocol={Protocol}", portAndProto.Port, portAndProto.Protocol);
                    if (!_ports.Contains(portAndProto))
                    {
                        _ports.Add(portAndProto);
                        _logger.LogInformation("Added Port={Port} Protocol={Protocol} to the list of forwarded ports", portAndProto.Port, portAndProto.Protocol);
                    }
                    else
                    {
                        _logger.LogInformation("Port={Port} Protocol={Protocol} is already in the list of forwarded ports", portAndProto.Protocol);
                    }
                }
                else
                {
                    _logger.LogInformation("Stop port forward for port={Port} protocol={Protocol}", portAndProto.Port, portAndProto.Protocol);
                    _ports.Remove(portAndProto);
                    _logger.LogInformation("Removed Port={Port} Protocol={Protocol} to the list of forwarded ports", portAndProto.Port, portAndProto.Protocol);
                }
                if (_configurationService.Configuration.PortForwardInterfaces.Count > 0 && ipAddress == IPAddress.Any)
                {
                    foreach (var networkInterface in NetworkInterface.GetAllNetworkInterfaces().Where(x => _configurationService.Configuration.PortForwardInterfaces.Contains(x.Id)))
                    {
                        EnablePortForwardingInterface(networkInterface, new[] { portAndProto }, enabled);
                    }
                }
                else
                {
                    _logger.LogInformation("No interfaces enabled to port forward to. Doing nothing");
                }
            }
        }
        catch (Exception ex)
        {
            // Just swallow, port forwarding is best effort
            _logger.LogError(ex, $"Failed to process port forward message: {ex.Message}");
        }
    }

    private void StopPortForwarding()
    {
        try
        {
            foreach (var e in _portForwarders)
            {
                _logger.LogInformation($"Stop port forwarding {e.Key}");
                e.Value.Stop();
            }
        }
        catch(Exception ex)
        {
            // Just swallow, port forwarding is best effort
            _logger.LogError(ex, $"Failed to stop port forwarding: {ex.Message}");
        }
        _portForwarders.Clear();
    }

    private static (IPAddress ipAddress, PortAndProtocol portAndProto) ParsePortForwardCmdLine(string cmdLine)
    {
        var m = _hostipParser.Match(cmdLine);
        var ipAddress = m.Success && m.Groups["h"].Value == "::" ? IPAddress.IPv6Any : IPAddress.Any;
        m = _hostPortParser.Match(cmdLine);
        var port = m.Success && int.TryParse(m.Groups["p"].Value, out var v) ? v : 0;
        m = _protoParser.Match(cmdLine);
        var proto = m.Success ? m.Groups["p"].Value : "tcp";
        return (ipAddress, new(port, proto));
    }

    private void InitializeDataDistro()
    {
        var task = Task.Run(async () =>
        {
            while (!_cts.IsCancellationRequested)
            {
                await _wslService.ExecuteCommandAsync($"/wsl-init-data.sh", _productInformation.ContainerDesktopDataDistroName, cancellationToken: _cts.Token);
                if (!_cts.IsCancellationRequested)
                {
                    await Task.Delay(1000);
                }
            }
        });
        _dataDistroInitTask = task;
        if(!WaitForDataDistroInitialization(5000))
        {
            throw new ContainerEngineException($"Could not initialize data distribution.");
        }
    }

    private void InitializeDnsConfigurator()
    {
        _dnsConfigurator?.Dispose();
        _dnsConfigurator = new DnsConfigurator(_wslService, _configurationService, _productInformation, _logger);
        _dnsConfigurator.Configure();
    }

    private void StopDnsConfigurator()
    {
        _dnsConfigurator?.Dispose();
    }

    public void Stop()
    {
        try
        {
            RunningState = RunningState.Stopping;
            StopDnsConfigurator();
            StopPortForwarding();
            StopDistros();
            StopProxy();
            _cts.Cancel();
            StopDaemon();
            _wslService.Terminate(_productInformation.ContainerDesktopDistroName);
            _wslService.Terminate(_productInformation.ContainerDesktopDataDistroName);
        }
        finally
        {
            RunningState = RunningState.Stopped;
        }
    }

    private void StopDaemon()
    {
        if(_wslService.ExecuteCommand($"pkill -TERM dockerd", _productInformation.ContainerDesktopDistroName))
        {
            _ = SpinWait.SpinUntil(() => !_wslService.ExecuteCommand("pgrep dockerd", _productInformation.ContainerDesktopDistroName), TimeSpan.FromSeconds(5));
        }
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
            var cts = CancellationTokenSource.CreateLinkedTokenSource(_cts.Token);
            var task = Task.Run(() => _wslService.ExecuteCommandAsync($"/mnt/wsl/container-desktop/distro/wsl-distro-init.sh \"{name}\"", name, "root", cancellationToken: cts.Token));
            
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
        try
        {
            Stop();
            ConfigurationChangedEventManager.RemoveHandler(_configurationService, OnConfigurationChanged);
        }
        finally
        {
            _proxyProcess?.Dispose();
        }
    }

    private void InitializeAndStartDaemon()
    {
        var certPath = GetWslPathInDistro(LocalCertsPath);
        if (!_wslService.ExecuteCommand($"/usr/local/bin/wsl-init.sh \"{certPath}\"", _productInformation.ContainerDesktopDistroName))
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


    private string LocalCertsPath { get; }
    
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
            .Add("--tls-key", Path.Combine(LocalCertsPath, "key.pem"), true)
            .Add("--tls-cert", Path.Combine(LocalCertsPath, "cert.pem"), true)
            .Add("--tls-ca", Path.Combine(LocalCertsPath, "ca.pem"), true)
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

    private static void WarmupDaemon()
    {
        //TODO: make client available in DI with all configuration set.
        Task.Run(() =>
        {
            using var client = new DockerClientConfiguration().CreateClient();
            client.Containers.ListContainersAsync(new Docker.DotNet.Models.ContainersListParameters());
        });
    }

    private bool WaitForDataDistroInitialization(int timeoutMs)
    {
        bool tailIsRunning = false;
        using var cts = new CancellationTokenSource(timeoutMs);
        while (!cts.IsCancellationRequested && !tailIsRunning)
        {
            _wslService.ExecuteCommand("ps -o comm", _productInformation.ContainerDesktopDataDistroName, stdout: s => tailIsRunning = s.StartsWith("tail"));
        }
        return cts.IsCancellationRequested;
    }

    public void EnablePortForwardingInterface(NetworkInterface networkInterface, bool enabled)
    {
        EnablePortForwardingInterface(networkInterface, _ports, enabled);
    }

    public void EnablePortForwardingInterface(NetworkInterface networkInterface, IEnumerable<PortAndProtocol> ports, bool enabled)
    {
        var ipProps = networkInterface.GetIPProperties();
        var addresses = ipProps.UnicastAddresses.Where(x => x.Address.AddressFamily == AddressFamily.InterNetwork).Select(x => x.Address);
        foreach (var portAndProto in ports)
        {
            foreach (var address in addresses)
            {
                try
                {
                    var key = $"{address}:{portAndProto.Port}:{portAndProto.Protocol}";
                    if (_portForwarders.TryGetValue(key, out var existing))
                    {
                        _logger.LogInformation("Stop port forwarding on {Address}:{Port}:{Protocol}", address.ToString(), portAndProto.Port, portAndProto.Protocol);
                        existing.Stop();
                        _portForwarders.Remove(key);
                    }

                    if (enabled)
                    {
                        var forwarder = new PortForwarder(_processExecutor);
                        _logger.LogInformation("Start port forwarding on {Address}:{Port}:{Protocol}", address.ToString(), portAndProto.Port, portAndProto.Protocol);
                        forwarder.Start(new IPEndPoint(address, portAndProto.Port), new IPEndPoint(IPAddress.Loopback, portAndProto.Port), portAndProto.Protocol);
                        _portForwarders.Add(key, forwarder);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to forward port on {Address}:{Port}:{Protocol}: {Message}", address.ToString(), portAndProto.Port, portAndProto.Protocol, ex.Message);
                }
            }
        }
    }

    private async void OnConfigurationChanged(object sender, ConfigurationChangedEventArgs e)
    {
        if (RunningState == RunningState.Running && e.PropertiesChanged.Contains(nameof(IContainerDesktopConfiguration.PortForwardingEnabled)))
        {
            try
            {
                if (_configurationService.Configuration.PortForwardingEnabled)
                {
                    _logger.LogInformation($"{nameof(IContainerDesktopConfiguration.PortForwardingEnabled)} setting changed to true, trying to initialize port forwarding");
                    InitializePortForwardListener();
                }
                else
                {
                    _logger.LogInformation($"{nameof(IContainerDesktopConfiguration.PortForwardingEnabled)} setting changed to false, trying to stop port forwarding");
                    if (_portforwardListenerCts != null)
                    {
                        _portforwardListenerCts.Cancel();
                        await _portForwardListenerTask;
                    }
                    if (!_wslService.ExecuteCommand("cp /usr/local/bin/docker-proxy-org /usr/local/bin/docker-proxy", _productInformation.ContainerDesktopDistroName, stdout: s => _logger.LogInformation(s)))
                    {
                        _logger.LogError("Could not disable port forwarding");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to change the {nameof(IContainerDesktopConfiguration.PortForwardingEnabled)} setting.");
            }
        }
        if(e.PropertiesChanged.Contains(nameof(ContainerDesktopConfiguration.Certificates)))
        {
            _ = Task.Run(() => UpdateCertificates());
        }
    }
}

#pragma warning restore CA2254
