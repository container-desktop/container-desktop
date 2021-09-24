namespace ContainerDesktop.Common.Cli;

public interface IRunner : IDisposable
{
    IServiceProvider ServiceProvider { get; }
    Task<int> RunAsync(string[] args, params Type[] optionTypes);
}
