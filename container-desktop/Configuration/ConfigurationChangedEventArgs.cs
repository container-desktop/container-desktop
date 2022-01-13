namespace ContainerDesktop.Configuration;

public class ConfigurationChangedEventArgs : EventArgs
{
    public ConfigurationChangedEventArgs(params string[] propertiesChanged)
    {
        PropertiesChanged = propertiesChanged ?? Array.Empty<string>();
    }

    public IReadOnlyCollection<string> PropertiesChanged { get; }
}
