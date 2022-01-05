using System.Text;

namespace ContainerDesktop.Wsl;

public static class WslServiceExtensions
{
    public static bool ExecuteCommand(this IWslService wslService, string command, string distro, StringBuilder stdOut = null, StringBuilder stdErr = null, string user = null)
    {
        return wslService.ExecuteCommand(command, distro, user, WriteToStdOut, WriteToStdErr);

        void WriteToStdOut(string s)
        {
            stdOut?.Append(s);
            stdOut?.Append('\n');
        }

        void WriteToStdErr(string s)
        {
            if(stdErr != null)
            {
                stdErr.Append(s);
                stdErr.Append('\n');
            }
            else
            {
                WriteToStdOut(s);
            }
        }
    }
}
