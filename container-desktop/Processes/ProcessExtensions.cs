namespace ContainerDesktop.Processes;

public static class ProcessExtensions
{
    public static async Task<int> CompleteAsync(this Process process, CancellationToken cancellationToken = default)
    {
        await process.WaitForExitAsync(cancellationToken);
        if(cancellationToken.IsCancellationRequested)
        {
            process.Kill();
        }
        return process.ExitCode;
    }

    public static int Complete(this Process process, CancellationToken cancellationToken = default)
    {
        ManualResetEvent mre = new(false);
        Task.Run(async () =>
        {
            await process.WaitForExitAsync(cancellationToken);
            if (cancellationToken.IsCancellationRequested)
            {
                process.Kill();
            }
            mre.Set();
        }, CancellationToken.None);

        mre.WaitOne();
        return process.ExitCode;
    }

    public static Process StartProcess(
        string command,
        string args,
        string workingDir = null,
        bool useShellExecute = false,
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
                    RedirectStandardError = !useShellExecute,
                    RedirectStandardOutput = !useShellExecute,
                    RedirectStandardInput = !useShellExecute,
                    UseShellExecute = useShellExecute,
                    CreateNoWindow = true,
                    StandardErrorEncoding = useShellExecute ? null : GetEncoding(),
                    StandardOutputEncoding = useShellExecute ? null : GetEncoding()
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

        if (!useShellExecute)
        {
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
                        stdErr(data);
                    }
                };
            }
        }

        process.Start();

        if (!useShellExecute)
        {
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
        }

        return process;
    }

    /// <summary>
    /// Launches <paramref name="fileName"/> as the interactive desktop user by delegating to the
    /// Shell.Application COM object. This avoids opening handles to other processes or duplicating
    /// tokens — operations that security software such as Microsoft Defender may block.
    /// Returns null because COM ShellExecute is fire-and-forget; no Process handle is available.
    /// </summary>
    public static Process RunAsDesktopUser(string fileName, string args = null, string workingDirectory = null)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(fileName));

        try
        {
            var shellType = Type.GetTypeFromProgID("Shell.Application");
            if (shellType == null)
                return null;

            dynamic shell = Activator.CreateInstance(shellType);
            shell.ShellExecute(fileName, args ?? "", workingDirectory ?? "", "", 1);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"RunAsDesktopUser failed for '{fileName}': {ex.Message}");
        }

        return null;
    }

    private static Encoding GetEncoding()
    {
        var info = Encoding.GetEncodings().FirstOrDefault(x => x.CodePage == CultureInfo.CurrentCulture.TextInfo.OEMCodePage);
        return info?.GetEncoding();
    }
}
