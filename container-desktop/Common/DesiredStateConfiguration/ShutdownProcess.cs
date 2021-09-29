using System.Diagnostics;

namespace ContainerDesktop.Common.DesiredStateConfiguration;

public class ShutdownProcess : ResourceBase
{
    public string FileName { get; set; }

    public ShutdownKind Kind { get; set; }

    public int WaitForExitTimeoutMs { get; set; } = 5000;

    private string ExpandedPath => Environment.ExpandEnvironmentVariables(FileName);

    public override void Set(ConfigurationContext context)
    {
        var processes = GetProcesses();
        foreach(var process in processes)
        {
            switch(Kind)
            {
                case ShutdownKind.MainWindow:
                    context.Logger.LogInformation("Try to close process '{ProcessName}' started at '{FileName}' by closing its main window.", process.ProcessName, process.MainModule.FileName);
                    process.CloseMainWindow();
                    break;
                default:
                    context.Logger.LogInformation("Try to close process '{ProcessName}' started at '{FileName}' by killing the process.", process.ProcessName, process.MainModule.FileName);
                    break;
            }
            if(!process.WaitForExit(WaitForExitTimeoutMs))
            {
                context.Logger.LogInformation("Killing process '{ProcessName}' started at '{FileName}'.", process.ProcessName, process.MainModule.FileName); 
                process.Kill(true);
            }
        }
    }

    public override bool Test(ConfigurationContext context)
    {
        return !GetProcesses().Any();
    }

    private IEnumerable<Process> GetProcesses()
    {
        var processName = Path.GetFileNameWithoutExtension(ExpandedPath);
        return Process.GetProcessesByName(processName).Where(x => x.MainModule.FileName.Equals(ExpandedPath, StringComparison.OrdinalIgnoreCase));
    }
}

