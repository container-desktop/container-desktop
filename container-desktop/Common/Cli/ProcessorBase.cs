namespace ContainerDesktop.Common.Cli;

public abstract class ProcessorBase<TOptions> : IProcessor<TOptions>
{
    protected ProcessorBase(TOptions options, ILogger logger)
    {
        Options = options ?? throw new ArgumentNullException(nameof(options));
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public TOptions Options { get; }

    public ILogger Logger { get; }

    public async Task<int> ProcessAsync()
    {
        try
        {
            await ProcessCoreAsync();
            return 0;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, ex.Message);
            return 1;
        }
    }

    protected abstract Task ProcessCoreAsync();
}
