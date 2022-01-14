namespace ContainerDesktop.Configuration;

public interface IConfigurationService : INotifyConfigurationChanged
{
    IContainerDesktopConfiguration Configuration { get; }
    bool IsChanged();
    void Load();
    void Save();
}
