using Serilog.Events;

namespace ContainerDesktop.Services;

public interface ILogObserver
{
    void SubscribeTo(IObservable<LogEvent> observable);
}