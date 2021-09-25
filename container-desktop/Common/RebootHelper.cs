namespace ContainerDesktop.Common;

using System.Diagnostics;
using System.Runtime.InteropServices;


public static class RebootHelper
{
    public static bool RequestReboot(bool restartApplications = true, IEnumerable<string> installerRestartArguments = null)
    {
        if (Process.GetCurrentProcess().AddShutdownPrvileges())
        {
            var exitFlags = ExitWindows.Reboot;
            if (restartApplications)
            {
                var args = Environment.GetCommandLineArgs().ToList();
                // Only occurs in the debugger.
                if (Path.GetExtension(args[0]) == ".dll")
                {
                    args[0] = Path.ChangeExtension(args[0], ".exe");
                }
                if (installerRestartArguments != null)
                {
                    var argsToAdd = installerRestartArguments.Except(args);
                    args.AddRange(argsToAdd);
                }
                var cmdLine = string.Join(" ", args);
                RegisterApplicationRestart(cmdLine, 0);
                exitFlags |= ExitWindows.RestartApps;
            }
            var shutdownReason = ShutdownReason.FlagPlanned | ShutdownReason.MajorSoftware | ShutdownReason.MinorInstallation;
            return ExitWindowsEx(exitFlags, shutdownReason);
        }
        return false;
    }

    private static bool AddShutdownPrvileges(this Process process)
    {
        IntPtr token;
        if (!OpenProcessToken(process.Handle, TOKEN_ADJUST_PRIVILEGES, out token))
        {
            return false;
        }
        LUID lid = new LUID();
        if (!LookupPrivilegeValue(null, SE_SHUTDOWN_NAME, ref lid))
        {
            return false;
        }
        LUID_AND_ATTRIBUTES att = new LUID_AND_ATTRIBUTES
        {
            Attributes = SE_PRIVILEGE_ENABLED,
            Luid = lid
        };
        TOKEN_PRIVILEGES tp = new TOKEN_PRIVILEGES
        {
            PrivilegeCount = 1,
            Privileges = new LUID_AND_ATTRIBUTES[] { att }
        };
        if (!AdjustTokenPrivileges(token, false, ref tp, 0, IntPtr.Zero, IntPtr.Zero))
        {
            return false;
        }
        return true;
    }

    [DllImport("advapi32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool AdjustTokenPrivileges(IntPtr tokenHandle, [MarshalAs(UnmanagedType.Bool)] bool disableAllPrivileges, ref TOKEN_PRIVILEGES newState, uint Zero, IntPtr null1, IntPtr null2);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool ExitWindowsEx(ExitWindows uFlags, ShutdownReason dwReason);

    [DllImport("advapi32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool OpenProcessToken(IntPtr processHandle, uint desiredAccess, out IntPtr tokenHandle);

    [DllImport("advapi32.dll")]
    private static extern bool LookupPrivilegeValue(string lpSystemName, string lpName, ref LUID lpLuid);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern int RegisterApplicationRestart([MarshalAs(UnmanagedType.LPWStr)] string commandLineArgs, int Flags);

    [Flags]
    private enum ExitWindows : uint
    {
        // ONE of the following five:
        LogOff = 0x00,
        ShutDown = 0x01,
        Reboot = 0x02,
        PowerOff = 0x08,
        RestartApps = 0x40,
        // plus AT MOST ONE of the following two:
        Force = 0x04,
        ForceIfHung = 0x10,
    }

    [Flags]
    private enum ShutdownReason : uint
    {
        MajorApplication = 0x00040000,
        MajorHardware = 0x00010000,
        MajorLegacyApi = 0x00070000,
        MajorOperatingSystem = 0x00020000,
        MajorOther = 0x00000000,
        MajorPower = 0x00060000,
        MajorSoftware = 0x00030000,
        MajorSystem = 0x00050000,

        MinorBlueScreen = 0x0000000F,
        MinorCordUnplugged = 0x0000000b,
        MinorDisk = 0x00000007,
        MinorEnvironment = 0x0000000c,
        MinorHardwareDriver = 0x0000000d,
        MinorHotfix = 0x00000011,
        MinorHung = 0x00000005,
        MinorInstallation = 0x00000002,
        MinorMaintenance = 0x00000001,
        MinorMMC = 0x00000019,
        MinorNetworkConnectivity = 0x00000014,
        MinorNetworkCard = 0x00000009,
        MinorOther = 0x00000000,
        MinorOtherDriver = 0x0000000e,
        MinorPowerSupply = 0x0000000a,
        MinorProcessor = 0x00000008,
        MinorReconfig = 0x00000004,
        MinorSecurity = 0x00000013,
        MinorSecurityFix = 0x00000012,
        MinorSecurityFixUninstall = 0x00000018,
        MinorServicePack = 0x00000010,
        MinorServicePackUninstall = 0x00000016,
        MinorTermSrv = 0x00000020,
        MinorUnstable = 0x00000006,
        MinorUpgrade = 0x00000003,
        MinorWMI = 0x00000015,

        FlagUserDefined = 0x40000000,
        FlagPlanned = 0x80000000
    }

    private struct TOKEN_PRIVILEGES
    {
        public int PrivilegeCount;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = ANYSIZE_ARRAY)]
        public LUID_AND_ATTRIBUTES[] Privileges;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    private struct LUID_AND_ATTRIBUTES
    {
        public LUID Luid;
        public uint Attributes;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct LUID
    {
        public uint LowPart;
        public uint HighPart;
    }

    private const int ANYSIZE_ARRAY = 1;
    private const uint SE_PRIVILEGE_ENABLED = 0x00000002;
    private const uint TOKEN_ADJUST_PRIVILEGES = 0x0020;
    private const string SE_SHUTDOWN_NAME = "SeShutdownPrivilege";
}
