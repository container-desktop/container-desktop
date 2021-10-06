namespace ContainerDesktop.Processes;

internal static class NativeMethods
{
    [DllImport("advapi32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool AdjustTokenPrivileges(SafeObjectHandle tokenHandle, [MarshalAs(UnmanagedType.Bool)] bool disableAllPrivileges, ref TOKEN_PRIVILEGES newState, uint Zero, IntPtr null1, IntPtr null2);

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
}
