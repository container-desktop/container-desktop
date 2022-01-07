namespace ContainerDesktop.Services;

public interface IConfigurationService : INotifyConfigurationChanged
{
    ContainerDesktopConfiguration Configuration { get; }
    bool IsChanged();
    void Load();
    void Save();
}
