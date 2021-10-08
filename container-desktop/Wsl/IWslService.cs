namespace ContainerDesktop.Wsl;

public interface IWslService
{
    bool Import(string distributionName, string installLocation, string rootfsFileName);
    bool Terminate(string distroName);
    bool Unregister(string distroName);
    bool ExecuteCommand(string command, string distroName, string user = null, Action<string> stdout = null, Action<string> stderr = null);
    Task<bool> ExecuteCommandAsync(string command, string distroName, string user = null, Action<string> stdout = null, Action<string> stderr = null, CancellationToken cancellationToken = default);
    bool IsInstalled(string distroName);
    IEnumerable<string> GetDistros();
}
