using System.Runtime.InteropServices;
using static PInvoke.Kernel32;
using static PInvoke.AdvApi32;

namespace ContainerDesktop.DesiredStateConfiguration;

internal static class NativeMethods
{
    [DllImport("advapi32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool AdjustTokenPrivileges(SafeObjectHandle tokenHandle, [MarshalAs(UnmanagedType.Bool)] bool disableAllPrivileges, ref TOKEN_PRIVILEGES newState, uint Zero, IntPtr null1, IntPtr null2);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool ExitWindowsEx(ExitWindows uFlags, ShutdownReason dwReason);

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern int RegisterApplicationRestart([MarshalAs(UnmanagedType.LPWStr)] string commandLineArgs, int Flags);

    [Flags]
    public enum ExitWindows : uint
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
    public enum ShutdownReason : uint
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

    public struct TOKEN_PRIVILEGES
    {
        public int PrivilegeCount;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = ANYSIZE_ARRAY)]
        public LUID_AND_ATTRIBUTES[] Privileges;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct LUID_AND_ATTRIBUTES
    {
        public PInvoke.User32.LUID Luid;
        public uint Attributes;
    }

    public const int ANYSIZE_ARRAY = 1;
    public const uint SE_PRIVILEGE_ENABLED = 0x00000002;
    public const string SE_SHUTDOWN_NAME = "SeShutdownPrivilege";
    public const uint TOKEN_ADJUST_PRIVILEGES = 0x0020;
    
    [DllImport("advapi32", SetLastError = true, CharSet = CharSet.Unicode)]
    public static extern bool CreateProcessWithTokenW(SafeObjectHandle hToken, LogonFlags dwLogonFlags, string lpApplicationName, string lpCommandLine, CreationFlags dwCreationFlags, IntPtr lpEnvironment, string lpCurrentDirectory, [In] ref STARTUPINFO lpStartupInfo, out PROCESS_INFORMATION lpProcessInformation);

    public enum LogonFlags
    {
        /// <summary>
        /// Log on, then load the user's profile in the HKEY_USERS registry key. The function
        /// returns after the profile has been loaded. Loading the profile can be time-consuming,
        /// so it is best to use this value only if you must access the information in the
        /// HKEY_CURRENT_USER registry key.
        /// NOTE: Windows Server 2003: The profile is unloaded after the new process has been
        /// terminated, regardless of whether it has created child processes.
        /// </summary>
        /// <remarks>See LOGON_WITH_PROFILE</remarks>
        WithProfile = 1,
        /// <summary>
        /// Log on, but use the specified credentials on the network only. The new process uses the
        /// same token as the caller, but the system creates a new logon session within LSA, and
        /// the process uses the specified credentials as the default credentials.
        /// This value can be used to create a process that uses a different set of credentials
        /// locally than it does remotely. This is useful in inter-domain scenarios where there is
        /// no trust relationship.
        /// The system does not validate the specified credentials. Therefore, the process can start,
        /// but it may not have access to network resources.
        /// </summary>
        /// <remarks>See LOGON_NETCREDENTIALS_ONLY</remarks>
        NetCredentialsOnly
    }

    public enum CreationFlags
    {
        DefaultErrorMode = 0x04000000,
        NewConsole = 0x00000010,
        NewProcessGroup = 0x00000200,
        SeparateWOWVDM = 0x00000800,
        Suspended = 0x00000004,
        UnicodeEnvironment = 0x00000400,
        ExtendedStartupInfoPresent = 0x00080000
    }

    public static bool AddPrvileges(IntPtr processHandle, params string[] privileges)
    {
        SafeObjectHandle token = null;
        if(privileges == null || privileges.Length == 0)
        {
            return false;
        }
        try
        {
            if (!OpenProcessToken(processHandle, TOKEN_ADJUST_PRIVILEGES, out token))
            {
                return false;
            }
            var atts = new LUID_AND_ATTRIBUTES[privileges.Length];
            for (var i = 0; i < privileges.Length; i++)
            {
                PInvoke.User32.LUID lid = new PInvoke.User32.LUID();
                
                if (!LookupPrivilegeValue(null, privileges[i], out lid))
                {
                    return false;
                }
                atts[i] = new LUID_AND_ATTRIBUTES
                {
                    Attributes = SE_PRIVILEGE_ENABLED,
                    Luid = lid
                };
            }
            TOKEN_PRIVILEGES tp = new TOKEN_PRIVILEGES
            {
                PrivilegeCount = atts.Length,
                Privileges = atts
            };
            if (!AdjustTokenPrivileges(token, false, ref tp, 0, IntPtr.Zero, IntPtr.Zero))
            {
                return false;
            }
            return true;
        }
        finally
        {
            token?.Close();
        }
    }
}
