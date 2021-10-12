namespace ContainerDesktop.DesiredStateConfiguration;

using System.Diagnostics;
using static NativeMethods;


public static class RebootHelper
{
    public static bool RequestReboot(bool restartApplications = true, IEnumerable<string> installerRestartArguments = null)
    {
        if (AddPrvileges(Process.GetCurrentProcess().Handle, SE_SHUTDOWN_NAME))
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
}
