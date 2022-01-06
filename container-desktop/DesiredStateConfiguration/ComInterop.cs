namespace ContainerDesktop.DesiredStateConfiguration;

using System.Runtime.InteropServices;

static class ComInterop
{
    public const int MAXPATH = 260;

    public const int INFOTIPSIZE = 1024;

    public const int STGMREAD = 0;

    public const uint SLGPUNCPRIORITY = 2u;

    [ComImport]
    [Guid("000214F9-0000-0000-C000-000000000046")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IShellLink
    {
        [PreserveSig]
        uint GetPath([Out][MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszFile, int cchMaxPath, ref WIN32_FIND_DATAW pfd, uint fFlags);

        [PreserveSig]
        uint GetIDList(out IntPtr ppidl);

        [PreserveSig]
        uint SetIDList(IntPtr pidl);

        [PreserveSig]
        uint GetDescription([Out][MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszName, int cchMaxName);

        [PreserveSig]
        uint SetDescription([MarshalAs(UnmanagedType.LPWStr)] string pszName);

        [PreserveSig]
        uint GetWorkingDirectory([Out][MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszDir, int cchMaxPath);

        [PreserveSig]
        uint SetWorkingDirectory([MarshalAs(UnmanagedType.LPWStr)] string pszDir);

        [PreserveSig]
        uint GetArguments([Out][MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszArgs, int cchMaxPath);

        [PreserveSig]
        uint SetArguments([MarshalAs(UnmanagedType.LPWStr)] string pszArgs);

        [PreserveSig]
        uint GetHotKey(out ushort pwHotkey);

        [PreserveSig]
        uint SetHotKey(ushort wHotKey);

        [PreserveSig]
        uint GetShowCmd(out int piShowCmd);

        [PreserveSig]
        uint SetShowCmd(int iShowCmd);

        [PreserveSig]
        uint GetIconLocation([Out][MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszIconPath, int cchIconPath, out int piIcon);

        [PreserveSig]
        uint SetIconLocation([MarshalAs(UnmanagedType.LPWStr)] string pszIconPath, int iIcon);

        [PreserveSig]
        uint SetRelativePath([MarshalAs(UnmanagedType.LPWStr)] string pszPathRel, uint dwReserved);

        [PreserveSig]
        uint Resolve(IntPtr hwnd, uint fFlags);

        [PreserveSig]
        uint SetPath([MarshalAs(UnmanagedType.LPWStr)] string pszFile);
    }

    [ComImport]
    [ClassInterface(ClassInterfaceType.None)]
    [Guid("00021401-0000-0000-C000-000000000046")]
    public class CShellLink
    {
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode, Pack = 4)]
    public struct WIN32_FIND_DATAW
    {
        public uint DwFileAttributes;

        public System.Runtime.InteropServices.ComTypes.FILETIME FtCreationTime;

        public System.Runtime.InteropServices.ComTypes.FILETIME FtLastAccessTime;

        public System.Runtime.InteropServices.ComTypes.FILETIME FtLastWriteTime;

        public uint NFileSizeHigh;

        public uint NFileSizeLow;

        public uint DwReserved0;

        public uint DwReserved1;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
        public string CFileName;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 14)]
        public string CAlternateFileName;
    }

    [ComImport]
    [Guid("886D8EEB-8CF2-4446-8D02-CDBA1DBDCF99")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IPropertyStore
    {
        uint GetCount(out uint cProps);

        uint GetAt([In] uint iProp, out PropertyKey pkey);

        uint GetValue([In] ref PropertyKey key, [Out] PropVariant pv);

        uint SetValue([In] ref PropertyKey key, [In] PropVariant pv);

        uint Commit();
    }

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct PropertyKey
    {
        private readonly Guid _formatId;

        public Guid FormatId => _formatId;

        public int PropertyId { get; }

        public PropertyKey(Guid formatId, int propertyId)
        {
            _formatId = formatId;
            PropertyId = propertyId;
        }

        public PropertyKey(string formatId, int propertyId)
        {
            _formatId = new Guid(formatId);
            PropertyId = propertyId;
        }
    }

    [StructLayout(LayoutKind.Explicit)]
    public sealed class PropVariant : IDisposable
    {
        [FieldOffset(0)]
        private ushort _valueType;

        [FieldOffset(8)]
        private readonly IntPtr _ptr;

        public VarEnum VarType
        {
            get
            {
                return (VarEnum)_valueType;
            }
            set
            {
                _valueType = (ushort)value;
            }
        }

        public bool IsNullOrEmpty
        {
            get
            {
                if (_valueType != 0)
                {
                    return _valueType == 1;
                }
                return true;
            }
        }

        public string Value => Marshal.PtrToStringUni(_ptr);

        public PropVariant()
        {
        }

        public PropVariant(string value)
        {
            if (value == null)
            {
                throw new ArgumentException("Failed to set value.");
            }
            _valueType = 31;
            _ptr = Marshal.StringToCoTaskMemUni(value);
        }

        ~PropVariant()
        {
            Dispose();
        }

        public void Dispose()
        {
            PropVariantClear(this);
            GC.SuppressFinalize(this);
        }
    }

    [DllImport("Ole32.dll", PreserveSig = false)]
    private static extern void PropVariantClear([In][Out] PropVariant pvar);


}
