namespace ContainerDesktop.Services
{
    public interface INotifyConfigurationChanged
    {
        event EventHandler<ConfigurationChangedEventArgs> ConfigurationChanged;
    }
}
