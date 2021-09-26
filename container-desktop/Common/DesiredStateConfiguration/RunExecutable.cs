namespace ContainerDesktop.Common.DesiredStateConfiguration;

public class RunExecutable : ResourceBase
{
    private readonly IProcessExecutor _processExecutor;

    public RunExecutable(IProcessExecutor processExecutor)
    {
        _processExecutor = processExecutor ?? throw new ArgumentNullException(nameof(processExecutor));
    }

    public string ExePath { get; set; }

    public List<string> Arguments { get; } = new List<string>();

    public bool Wait { get; set; } = true;

    public string WorkingDirectory { get; set; }

    private string ExpandedWorkingDirectory => WorkingDirectory == null ? null : Environment.ExpandEnvironmentVariables(WorkingDirectory);

    public Dictionary<string, string> EnvironmentVariables { get; } = new Dictionary<string, string>();

    public bool OnInstall { get; set; } = true;
    public bool OnUninstall { get; set; } = true;

    private string ExpandedExePath => Environment.ExpandEnvironmentVariables(ExePath);

    public override void Set(ConfigurationContext context)
    {
        var process = _processExecutor.Start(
            ExpandedExePath, 
            string.Join(" ", Arguments), 
            ExpandedWorkingDirectory, 
            null, null, 
            EnvironmentVariables.Select(x => (x.Key, x.Value)).ToArray());
        if(Wait)
        {
            process.Complete();
        }
    }

    public override bool Test(ConfigurationContext context)
    {
        return !((context.Uninstall && OnUninstall) || (!context.Uninstall && OnInstall));
    }
}

