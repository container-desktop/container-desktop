namespace ContainerDesktop.Common.DesiredStateConfiguration;

public interface IUserInteraction
{
    bool UserConsent(string message, string caption = null);
    void ReportProgress(int value, int max, string message);
    bool Uninstalling { get; set; }
}
