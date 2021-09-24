namespace ContainerDesktop.Common.Services;

public interface IWslService
{
    bool Import(string distributionName, string installLocation, string rootfsFileName);
    bool Terminate(string distroName);
    bool Unregister(string distroName);
    bool ExecuteCommand(string command, string distroName);
    bool IsInstalled(string distroName);
    IEnumerable<string> GetDistros();
}
