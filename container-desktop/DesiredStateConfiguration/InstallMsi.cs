using ContainerDesktop.Processes;
using Microsoft.Win32;
using System.Net.Http;

namespace ContainerDesktop.DesiredStateConfiguration;

public class InstallMsi : ResourceBase
{
    private const string UninstallRegistryKey = "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Uninstall";

    // 0     = success
    // 1638  = another version of this product is already installed — treat as success
    // 1641  = success, reboot initiated
    // 3010  = success, reboot required
    private static readonly HashSet<int> SuccessExitCodes = new() { 0, 1638, 1641, 3010 };

    private readonly IProcessExecutor _processExecutor;

    public InstallMsi(IProcessExecutor processExecutor)
    {
        _processExecutor = processExecutor ?? throw new ArgumentNullException(nameof(processExecutor));
    }

    public Uri Uri { get; set; }

    public string UninstallDisplayName { get; set; }

    public string FallbackPath { get; set; }

    private string ExpandedFallbackPath => Environment.ExpandEnvironmentVariables(FallbackPath);

    public override void Set(ConfigurationContext context) => Do(context, false);

    public override void Unset(ConfigurationContext context) => Do(context, true);

    private void Do(ConfigurationContext context, bool uninstall)
    {
        // msiexec requires a .msi extension — without it some versions reject the file
        var tmpFileName = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.msi");
        try
        {
            bool downloadedOk = false;
            try
            {
                using var client = new HttpClient();
                using var s = client.GetStreamAsync(Uri).GetAwaiter().GetResult();
                using var fs = context.FileSystem.File.Create(tmpFileName);
                s.CopyTo(fs);
                downloadedOk = true;
            }
            catch (Exception ex)
            {
                context.Logger.LogWarning("Failed to download MSI from {Uri}: {Error}. Falling back to bundled package.", Uri, ex.Message);
            }

            if (downloadedOk)
            {
                var exitCode = RunMsiExec(tmpFileName, uninstall);
                if (SuccessExitCodes.Contains(exitCode))
                {
                    LogResult(context, Uri.ToString(), exitCode);
                    return;
                }
                context.Logger.LogWarning("msiexec returned {ExitCode} for downloaded MSI from {Uri}. Falling back to bundled package.", exitCode, Uri);
            }

            // Fall back to the bundled MSI shipped with the installer
            if (!string.IsNullOrEmpty(FallbackPath) && File.Exists(ExpandedFallbackPath))
            {
                var exitCode = RunMsiExec(ExpandedFallbackPath, uninstall);
                if (!SuccessExitCodes.Contains(exitCode))
                {
                    throw new ResourceException($"Could not install msi package from '{ExpandedFallbackPath}'. msiexec exit code: {exitCode}.");
                }
                LogResult(context, ExpandedFallbackPath, exitCode);
            }
            else
            {
                throw new ResourceException($"Could not install msi package from '{Uri}' and no bundled fallback is available.");
            }
        }
        finally
        {
            if (context.FileSystem.File.Exists(tmpFileName))
            {
                context.FileSystem.File.Delete(tmpFileName);
            }
        }
    }

    private int RunMsiExec(string msiPath, bool uninstall)
    {
        var cmd = uninstall ? "/u" : "/i";
        return _processExecutor.Execute("msiexec.exe", $"{cmd} \"{msiPath}\" /quiet /norestart");
    }

    private static void LogResult(ConfigurationContext context, string source, int exitCode)
    {
        if (exitCode == 1638)
            context.Logger.LogInformation("MSI from '{Source}' is already installed (exit code 1638) — skipping.", source);
        else if (exitCode is 1641 or 3010)
            context.Logger.LogInformation("MSI from '{Source}' installed successfully. A restart is required (exit code {ExitCode}).", source, exitCode);
    }

    public override bool Test(ConfigurationContext context)
    {
        using var key = Registry.LocalMachine.OpenSubKey(UninstallRegistryKey);
        if (key == null) return false;

        foreach (var subKeyName in key.GetSubKeyNames())
        {
            using var subKey = key.OpenSubKey(subKeyName);
            var displayName = subKey?.GetValue("DisplayName") as string;
            if (displayName != null && displayName.Equals(UninstallDisplayName, StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }
}
