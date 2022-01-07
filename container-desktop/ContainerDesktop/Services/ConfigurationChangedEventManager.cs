using System.Windows;

namespace ContainerDesktop.Services;

public class ConfigurationChangedEventManager : WeakEventManager
{
    private ConfigurationChangedEventManager() { }

    public static void AddHandler(INotifyConfigurationChanged source, EventHandler<ConfigurationChangedEventArgs> handler)
    {
        if(source == null)
        {
            throw new ArgumentNullException(nameof(source));
        }
        if (handler == null)
        {
            throw new ArgumentNullException(nameof(handler));
        }
        CurrentManager.ProtectedAddHandler(source, handler);
    }

    public static void RemoveHandler(INotifyConfigurationChanged source, EventHandler<ConfigurationChangedEventArgs> handler)
    {
        if (source == null)
        {
            throw new ArgumentNullException(nameof(source));
        }
        if (handler == null)
        {
            throw new ArgumentNullException(nameof(handler));
        }
        CurrentManager.ProtectedRemoveHandler(source, handler);
    }

    private static ConfigurationChangedEventManager CurrentManager
    {
        get
        {
            var type = typeof(ConfigurationChangedEventManager);
            var manager = (ConfigurationChangedEventManager) GetCurrentManager(type);
            if(manager == null)
            {
                manager = new ConfigurationChangedEventManager();
                SetCurrentManager(type, manager);
            }
            return manager;
        }
    }

    protected override ListenerList NewListenerList()
    {
        return new ListenerList<ConfigurationChangedEventArgs>();
    }

    protected override void StartListening(object source)
    {
        INotifyConfigurationChanged typedSource = (INotifyConfigurationChanged) source;
        typedSource.ConfigurationChanged += new EventHandler<ConfigurationChangedEventArgs>(OnNotifyConfigurationChanged);
    }

    protected override void StopListening(object source)
    {
        INotifyConfigurationChanged typedSource = (INotifyConfigurationChanged)source;
        typedSource.ConfigurationChanged -= new EventHandler<ConfigurationChangedEventArgs>(OnNotifyConfigurationChanged);
    }

    private void OnNotifyConfigurationChanged(object sender, ConfigurationChangedEventArgs e)
    {
        DeliverEvent(sender, e);
    }
}
