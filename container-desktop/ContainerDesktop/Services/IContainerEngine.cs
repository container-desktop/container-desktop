﻿using System.Net.NetworkInformation;

namespace ContainerDesktop.Services;

public interface IContainerEngine
{
    event EventHandler RunningStateChanged;
    RunningState RunningState { get; }
    void Start();
    void Stop();
    void Restart();
    void EnableDistro(string name, bool enabled);
    void EnablePortForwardingInterface(NetworkInterface networkInterface, bool enabled);
}
