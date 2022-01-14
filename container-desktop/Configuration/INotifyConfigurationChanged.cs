namespace ContainerDesktop.Configuration
{
    public interface INotifyConfigurationChanged
    {
        event EventHandler<ConfigurationChangedEventArgs>? ConfigurationChanged;
    }
}
