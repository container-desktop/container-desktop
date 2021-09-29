namespace ContainerDesktop.Common;

using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using static ContainerDesktop.Common.ComInterop;


public class ShellLink : IDisposable
{
    private IShellLink _shellLinkW;

    private readonly PropertyKey _appUserModelIDKey = new PropertyKey("{9F4C2855-9F79-4B39-A8D0-E1D42DE1D5F3}", 5);

    public ShellLink() : this(null)
    {
    }

    public ShellLink(string fileName)
    {
        try
        {
            _shellLinkW = (IShellLink)new CShellLink();
        }
        catch
        {
            throw new COMException("Failed to create ShellLink object.");
        }
        if (fileName != null)
        {
            Load(fileName);
        }
    }

    ~ShellLink()
    {
        Dispose(disposing: false);
    }

    public string ShortcutFile
    {
        get
        {
            GetPersistFile().GetCurFile(out var shortcutFile);
            return shortcutFile;
        }
    }

    public string TargetPath
    {
        get
        {
            StringBuilder targetPath = new StringBuilder(260);
            WIN32_FIND_DATAW data = default(WIN32_FIND_DATAW);
            VerifySucceeded(_shellLinkW.GetPath(targetPath, targetPath.Capacity, ref data, 2u));
            return targetPath.ToString();
        }
        set
        {
            VerifySucceeded(_shellLinkW.SetPath(value));
        }
    }

    public string Description
    {
        get
        {
            StringBuilder desc = new StringBuilder(1024);
            VerifySucceeded(_shellLinkW.GetDescription(desc, desc.Capacity));
            return desc.ToString();
        }
        set
        {
            VerifySucceeded(_shellLinkW.SetDescription(value));
        }
    }

    public string IconPath
    {
        get
        {
            StringBuilder iconPath = new StringBuilder(260);
            VerifySucceeded(_shellLinkW.GetIconLocation(iconPath, iconPath.Capacity, out var _));
            return iconPath.ToString();
        }
        set
        {
            VerifySucceeded(_shellLinkW.SetIconLocation(value, 0));
        }
    }

    public string Arguments
    {
        get
        {
            StringBuilder arguments = new StringBuilder(1024);
            VerifySucceeded(_shellLinkW.GetArguments(arguments, arguments.Capacity));
            return arguments.ToString();
        }
        set
        {
            VerifySucceeded(_shellLinkW.SetArguments(value));
        }
    }

    public string AppUserModelID
    {
        get
        {
            using PropVariant pv = new PropVariant();
            IPropertyStore propertyStore = GetPropertyStore();
            PropertyKey key = _appUserModelIDKey;
            VerifySucceeded(propertyStore.GetValue(ref key, pv));
            if (pv.Value == null)
            {
                return "Null";
            }
            return pv.Value;
        }
        set
        {
            using PropVariant pv = new PropVariant(value);
            IPropertyStore propertyStore = GetPropertyStore();
            PropertyKey key = _appUserModelIDKey;
            VerifySucceeded(propertyStore.SetValue(ref key, pv));
            VerifySucceeded(propertyStore.Commit());
        }
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_shellLinkW != null)
        {
            Marshal.FinalReleaseComObject(_shellLinkW);
            _shellLinkW = null;
        }
    }

    public void Save()
    {
        string file = ShortcutFile;
        if (file == null)
        {
            throw new InvalidOperationException("File name is not given.");
        }
        Save(file);
    }

    public void Save(string fileName)
    {
        if (fileName == null)
        {
            throw new ArgumentNullException(nameof(fileName));
        }
        GetPersistFile().Save(fileName, fRemember: true);
    }

    public void Load(string fileName)
    {
        if (!File.Exists(fileName))
        {
            throw new FileNotFoundException("File is not found.", fileName);
        }
        GetPersistFile().Load(fileName, 0);
    }

    public static void VerifySucceeded(uint hresult)
    {
        if (hresult > 1)
        {
            throw new InvalidOperationException("Failed with HRESULT: " + hresult.ToString("X"));
        }
    }

    private IPersistFile GetPersistFile()
    {
        IPersistFile persistFile;
        if ((persistFile = _shellLinkW as IPersistFile) == null)
        {
            throw new InvalidCastException("Failed to create IPersistFile.");
        }
        return persistFile;
    }

    private IPropertyStore GetPropertyStore()
    {
        IPropertyStore propertyStore;
        if ((propertyStore = _shellLinkW as IPropertyStore) == null)
        {
            throw new InvalidCastException("Failed to create IPropertyStore.");
        }
        return propertyStore;
    }
}
