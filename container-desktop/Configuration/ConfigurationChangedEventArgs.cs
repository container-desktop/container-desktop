namespace ContainerDesktop.Configuration;

public class ConfigurationChangedEventArgs : EventArgs
{
    public ConfigurationChangedEventArgs(bool restartRequested, params string[] propertiesChanged)
    {
        RestartRequested = restartRequested;
        PropertiesChanged = propertiesChanged ?? Array.Empty<string>();
    }

    public IReadOnlyCollection<string> PropertiesChanged { get; }

    public bool RestartRequested { get; }
}
