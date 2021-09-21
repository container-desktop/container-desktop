using System.Threading.Tasks;

namespace ContainerDesktop.Common.Cli
{
    public interface IProcessor
    {
        Task<int> ProcessAsync();
    }

    public interface IProcessor<out TOptions> : IProcessor
    {
        TOptions Options { get; }
    }
}
