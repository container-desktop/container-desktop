namespace ContainerDesktop.Common.DesiredStateConfiguration;

public interface IUserInteraction
{
    bool UserConsent(string message, string caption = null);
    void ReportProgress(int value, int max, string message, string extraInformation = null);
    bool Uninstalling { get; set; }
}
