namespace ContainerDesktop.Common.DesiredStateConfiguration;

using ContainerDesktop.Common.Services;

public class WslDistro : ResourceBase
{
    private readonly IWslService _wslService;

    public WslDistro(IWslService wslService)
    {
        _wslService = wslService ?? throw new ArgumentNullException(nameof(wslService));
    }

    public string Name { get; set; }
    public string Path { get; set; }
    public string RootfsFileName { get; set; }

    private string ExpandedPath => Environment.ExpandEnvironmentVariables(Path);


    public override void Set(ConfigurationContext context)
    {
        var path = ExpandedPath;
        if (!context.FileSystem.Directory.Exists(path))
        {
            context.FileSystem.Directory.CreateDirectory(path);
        }
        var rootfsFileName = Environment.ExpandEnvironmentVariables(RootfsFileName);
        if (!_wslService.Import(Name, path, rootfsFileName))
        {
            throw new ResourceException($"Could not import distribution '{Name}' from '{rootfsFileName}' to '{path}'. \r\n Please ensure virtualization is enabled in the BIOS or that NestedVirtualization is enabled when installing on a Virtual Machine.");
        }
    }

    public override void Unset(ConfigurationContext context)
    {
        if (_wslService.IsInstalled(Name))
        {
            _wslService.Unregister(Name);
        }
        if (context.FileSystem.Directory.Exists(ExpandedPath))
        {
            context.FileSystem.Directory.Delete(ExpandedPath, true);
        }
    }

    public override bool Test(ConfigurationContext context)
    {
        var isInstalled = _wslService.IsInstalled(Name);
        var pathExists = context.FileSystem.Directory.Exists(ExpandedPath);
        return isInstalled && pathExists;
    }
}
