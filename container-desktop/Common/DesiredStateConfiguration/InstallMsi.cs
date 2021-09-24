using Microsoft.Win32;
using System.Net;

namespace ContainerDesktop.Common.DesiredStateConfiguration;

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


    public override void Set(ConfigurationContext context)
    {
        var tmpFileName = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        try
        {

            var request = WebRequest.Create(Uri);
            using (var response = request.GetResponse())
            using (var s = response.GetResponseStream())
            using (var fs = context.FileSystem.File.Create(tmpFileName))
            {
                s.CopyTo(s);
            }
            var cmd = context.Uninstall ? "/u" : "/i";
            var exitCode = _processExecutor.Execute("msiexec.exe", $"{cmd} \"{tmpFileName}\" /quiet /norestart");
            if (exitCode != 0)
            {
                throw new ResourceException($"Could not install msi package from '{Uri}'.");
            }
        }
        finally
        {
            if(context.FileSystem.File.Exists(tmpFileName))
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
        if(context.Uninstall)
        {
            installed = !installed;
        }
        return installed;
    }
}
