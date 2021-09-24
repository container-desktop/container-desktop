namespace ContainerDesktop.Common;

public interface IApplicationContext
{
    int ExitCode { get; set; }
    void QuitApplication();
    void ShowMainWindow();
}