using System.Collections.Generic;

namespace ContainerDesktop.Common.Services
{
    public interface IWslService
    {
        string ContainerDesktopDistroName { get; }
        bool IsWslInstalled();
        bool InstallWsl();
        bool Import(string installLocation, string rootfsFileName);
        bool Import(string distributionName, string installLocation, string rootfsFileName);
        bool Terminate();
        bool Unregister();
        bool Unregister(string distroName);
        bool ExecuteCommand(string command);
        bool ExecuteCommand(string command, string distroName);
        bool IsInstalled(string distroName);
        bool IsInstalled();
        IEnumerable<string> GetDistros();
    }
}
