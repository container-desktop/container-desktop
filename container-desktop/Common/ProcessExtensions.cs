using System;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ContainerDesktop.Common
{
    public static class ProcessExtensions
    {
        public static async Task<int> CompleteAsync(this Process process, CancellationToken cancellationToken = default)
        {
            await process.WaitForExitAsync(cancellationToken);
            return process.ExitCode;
        }

        public static int Complete(this Process process, CancellationToken cancellationToken = default)
        {
            ManualResetEvent mre = new ManualResetEvent(false);
            Task.Run(async () =>
            {
                await process.WaitForExitAsync(cancellationToken);
                mre.Set();
            });
            
            mre.WaitOne();
            return process.ExitCode;
        }

        public static Process StartProcess(
            string command,
            string args,
            string workingDir = null,
            Action<string> stdOut = null,
            Action<string> stdErr = null,
            params (string key, string value)[] environmentVariables)
        {
            args ??= "";

            var process = new Process
            {
                StartInfo =
                {
                    Arguments = args,
                    FileName = command,
                    RedirectStandardError = true,
                    RedirectStandardOutput = true,
                    RedirectStandardInput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    StandardErrorEncoding = Encoding.GetEncoding(CultureInfo.CurrentCulture.TextInfo.OEMCodePage)
                }
            };

            if (!string.IsNullOrWhiteSpace(workingDir))
            {
                process.StartInfo.WorkingDirectory = workingDir;
            }

            if (environmentVariables.Length > 0)
            {
                for (var i = 0; i < environmentVariables.Length; i++)
                {
                    var (key, value) = environmentVariables[i];
                    process.StartInfo.Environment.Add(key, value);
                }
            }

            if (stdOut != null)
            {
                process.OutputDataReceived += (sender, eventArgs) =>
                {
                    if (eventArgs.Data != null)
                    {
                        var data = eventArgs.Data.Replace("\0", "");
                        stdOut(data);
                    }
                };
            }

            if (stdErr != null)
            {
                process.ErrorDataReceived += (sender, eventArgs) =>
                {
                    if (eventArgs.Data != null)
                    {
                        var data = eventArgs.Data.Replace("\0", "");
                        stdErr(eventArgs.Data);
                    }
                };
            }

            process.Start();

            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            return process;
        }
    }
}
