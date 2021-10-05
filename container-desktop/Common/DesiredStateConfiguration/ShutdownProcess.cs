using System.Diagnostics;
using System.Runtime.InteropServices;

namespace ContainerDesktop.Common.DesiredStateConfiguration;

public class ShutdownProcess : ResourceBase
{
    public string FileName { get; set; }

    public ShutdownKind Kind { get; set; }

    public int CustomMessage { get; set; }

    public string WindowTitle { get; set; }

    public int WaitForExitTimeoutMs { get; set; } = 5000;

    private string ExpandedPath => Environment.ExpandEnvironmentVariables(FileName);

    public override void Set(ConfigurationContext context)
    {
        var processes = GetProcesses();
        foreach(var process in processes)
        {
            bool exited = false;
            switch(Kind)
            {
                case ShutdownKind.MainWindow:
                    context.Logger.LogInformation("Try to close process '{ProcessName}' started at '{FileName}' by closing its main window.", process.ProcessName, process.MainModule.FileName);
                    process.CloseMainWindow();
                    exited = process.WaitForExit(WaitForExitTimeoutMs);
                    break;
                case ShutdownKind.CustomMessage when CustomMessage >= (int)PInvoke.User32.WindowMessage.WM_APP:
                    var h = FindWindow(process);
                    if (h != IntPtr.Zero)
                    {
                        if(PInvoke.User32.PostMessage(h, (PInvoke.User32.WindowMessage)CustomMessage, IntPtr.Zero, IntPtr.Zero))
                        {
                            exited = process.WaitForExit(WaitForExitTimeoutMs);
                        }
                        else
                        {
                            var le = Marshal.GetLastWin32Error();
                        }
                    }
                    break;
                default:
                    context.Logger.LogInformation("Try to close process '{ProcessName}' started at '{FileName}' by killing the process.", process.ProcessName, process.MainModule.FileName);
                    break;
            }
            if(!exited)
            {
                context.Logger.LogInformation("Killing process '{ProcessName}' started at '{FileName}'.", process.ProcessName, process.MainModule.FileName); 
                process.Kill(true);
            }
        }
    }

    public override void Unset(ConfigurationContext context)
    {
        Set(context);
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

    private IntPtr FindWindow(Process process)
    {
        var handles = new List<IntPtr>();
        PInvoke.User32.EnumWindows((hwnd, lparam) =>
        {
            PInvoke.User32.GetWindowThreadProcessId(hwnd, out var processId);
            if(processId == process.Id)
            {
                handles.Add(hwnd);
            }
            return true;
        }, IntPtr.Zero);
        return string.IsNullOrEmpty(WindowTitle) ? 
            handles.FirstOrDefault() : 
            handles.FirstOrDefault(x => WindowTitle.Equals(PInvoke.User32.GetWindowText(x), StringComparison.OrdinalIgnoreCase));
    }
}

