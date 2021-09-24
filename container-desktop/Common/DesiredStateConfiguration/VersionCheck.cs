using Microsoft.Win32;

namespace ContainerDesktop.Common.DesiredStateConfiguration;

public class VersionCheck : ResourceBase
{
    private const string CurrentVersionKey = @"SOFTWARE\Microsoft\Windows NT\CurrentVersion";
    private const string CurrentMajorVersionNumber = nameof(CurrentMajorVersionNumber);
    private const string CurrentMinorVersionNumber = nameof(CurrentMinorVersionNumber); 
    private const string CurrentBuildNumber = nameof(CurrentBuildNumber);
    private const string CurrentMinorBuildNumber = "UBR";

    public override void Set(ConfigurationContext context)
    {
        using var key = Registry.LocalMachine.OpenSubKey(CurrentVersionKey);
        if (key != null)
        {
            var minorVersionNumber = (int)key.GetValue(CurrentMinorVersionNumber, 0);
            var majorVersionNumber = (int) key.GetValue(CurrentMajorVersionNumber, 0);
            int.TryParse((string)key.GetValue(CurrentBuildNumber, "0"), out var buildNumber);
            var minorBuildNumber = (int)key.GetValue(CurrentMinorBuildNumber, 0);

            if(majorVersionNumber >= 10)
            {
                if(buildNumber >= 19041 || ((buildNumber == 18362 || buildNumber == 18363) && minorBuildNumber >= 1049))
                {
                    return;
                }
            }
            throw new ResourceException($"{Product.Name} is not supported on your version of Windows ({majorVersionNumber}.{minorVersionNumber}.{buildNumber}.{minorBuildNumber}).");
        }
    }

    public override bool Test(ConfigurationContext context)
    {
        return false;
    }
}

