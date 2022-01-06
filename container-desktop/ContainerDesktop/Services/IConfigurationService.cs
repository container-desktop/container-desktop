namespace ContainerDesktop.Services;

public interface IConfigurationService
{
    ContainerDesktopConfiguration Configuration { get; }
    void Load();
    void Save();
}
