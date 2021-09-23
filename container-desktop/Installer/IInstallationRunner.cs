using System.Threading.Tasks;

namespace ContainerDesktop.Installer
{
    public interface IInstallationRunner
    {
        InstallationMode InstallationMode { get; }
        Task<int> RunAsync();
    }
}
