﻿namespace ContainerDesktop.Common;

public interface IApplicationContext
{
    int ExitCode { get; set; }
    string LastErrorMessage { get; set; }
    void QuitApplication();
    void ShowMainWindow();
    void ShowSettings();
    void InvokeOnDispatcher(Action action);
}