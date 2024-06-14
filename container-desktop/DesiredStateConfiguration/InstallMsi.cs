using ContainerDesktop.Processes;
using Microsoft.Win32;
using System.Net.Http;

namespace ContainerDesktop.DesiredStateConfiguration;

public class InstallMsi : ResourceBase
{
    private const string UninstallRegistryKey = "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Uninstall";
    private readonly IProcessExecutor _processExecutor;

    public InstallMsi(IProcessExecutor processExecutor)
    {
        _processExecutor = processExecutor ?? throw new ArgumentNullException(nameof(processExecutor));
    }

    public Uri Uri { get; set; }

    public string UninstallDisplayName { get; set; }

    public string FallbackPath { get; set; }

    private string ExpandedFallbackPath => Environment.ExpandEnvironmentVariables(FallbackPath);

    public override void Set(ConfigurationContext context)
    {
        Do(context, false);
    }

    public override void Unset(ConfigurationContext context)
    {
        Do(context, true);
    }

    private void Do(ConfigurationContext context, bool uninstall)
    {
        var tmpFileName = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        try
        {
            using (var client = new HttpClient())
            using (var s = client.GetStreamAsync(Uri).GetAwaiter().GetResult())
            using (var fs = context.FileSystem.File.Create(tmpFileName))
            {
                s.CopyTo(fs);
            }
            var cmd = uninstall ? "/u" : "/i";
            var exitCode = _processExecutor.Execute("msiexec.exe", $"{cmd} \"{tmpFileName}\" /quiet /norestart");
            if (exitCode != 0)
            {
                throw new ResourceException($"Could not install msi package from '{Uri}'.");
            }
        }
        catch
        {
            if (!string.IsNullOrEmpty(FallbackPath) && File.Exists(ExpandedFallbackPath))
            {
                var cmd = uninstall ? "/u" : "/i";
                var exitCode = _processExecutor.Execute("msiexec.exe", $"{cmd} \"{ExpandedFallbackPath}\" /quiet /norestart");
                if (exitCode != 0)
                {
                    throw new ResourceException($"Could not install msi package from '{ExpandedFallbackPath}'.");
                }
            }
            else
            {
                throw;
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

    public override bool Test(ConfigurationContext context)
    {
        var installed = false;
        using var key = Registry.LocalMachine.OpenSubKey(UninstallRegistryKey);
        if(key != null)
        {
            foreach(var subKeyName in key.GetSubKeyNames())
            {
                using var subKey = key.OpenSubKey(subKeyName);
                var displayName = (string)subKey.GetValue("DisplayName");
                if(displayName != null && displayName.Equals(UninstallDisplayName, StringComparison.OrdinalIgnoreCase))
                {
                    installed = true;
                    break;
                }
            }
        }
        return installed;
    }
}
