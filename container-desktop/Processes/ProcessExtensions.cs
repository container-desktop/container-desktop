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

    public static Process RunAsDesktopUser(string fileName, string args = null, string workingDirectory = null)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(fileName));
        }

        // To start process as shell user you will need to carry out these steps:
        // 1. Enable the SeIncreaseQuotaPrivilege in your current token
        // 2. Get an HWND representing the desktop shell (GetShellWindow)
        // 3. Get the Process ID(PID) of the process associated with that window(GetWindowThreadProcessId)
        // 4. Open that process(OpenProcess)
        // 5. Get the access token from that process (OpenProcessToken)
        // 6. Make a primary token with that token(DuplicateTokenEx)
        // 7. Start the new process with that primary token(CreateProcessWithTokenW)

        SafeObjectHandle hProcessToken = null;
        // Enable SeIncreaseQuotaPrivilege in this process.  (This won't work if current process is not elevated.)
        try
        {
            var process = GetCurrentProcess();
            if (!AdvApi32.OpenProcessToken(process.DangerousGetHandle(), 0x0020, out hProcessToken))
            {
                return null;
            }
            var tkp = new TOKEN_PRIVILEGES
            {
                PrivilegeCount = 1,
                Privileges = new LUID_AND_ATTRIBUTES[1]
            };
            
            if (!AdvApi32.LookupPrivilegeValue(null, "SeIncreaseQuotaPrivilege", out tkp.Privileges[0].Luid))
            {
                return null;
            }
            tkp.Privileges[0].Attributes = 0x00000002;

            if (!AdjustTokenPrivileges(hProcessToken, false, ref tkp, 0, IntPtr.Zero, IntPtr.Zero))
            {
                return null;
            }
        }
        finally
        {
            hProcessToken?.Close();
        }

        // Get an HWND representing the desktop shell.
        // CAVEATS:  This will fail if the shell is not running (crashed or terminated), or the default shell has been
        // replaced with a custom shell.  This also won't return what you probably want if Explorer has been terminated and
        // restarted elevated.
        var hwnd = User32.GetShellWindow();
        if (hwnd == IntPtr.Zero)
        {
            return null;
        }
        SafeObjectHandle hShellProcess = null;
        SafeObjectHandle hShellProcessToken = null;
        SafeObjectHandle hPrimaryToken = null;
        try
        {
            // Get the PID of the desktop shell process.
            if (User32.GetWindowThreadProcessId(hwnd, out int dwPID) == 0)
            {
                return null;
            }
            // Open the desktop shell process in order to query it (get the token)
            hShellProcess = OpenProcess(new ACCESS_MASK(0x00000400), false, dwPID);
            if (hShellProcess.DangerousGetHandle() == IntPtr.Zero)
            {
                return null;
            }
            // Get the process token of the desktop shell.
            if (!AdvApi32.OpenProcessToken(hShellProcess.DangerousGetHandle(), 0x0002, out hShellProcessToken))
            {
                return null;
            }
            var dwTokenRights = 395U;

            // Duplicate the shell's process token to get a primary token.
            // Based on experimentation, this is the minimal set of rights required for CreateProcessWithTokenW (contrary to current documentation).
            if (!AdvApi32.DuplicateTokenEx(hShellProcessToken, dwTokenRights, IntPtr.Zero, SECURITY_IMPERSONATION_LEVEL.SecurityImpersonation, AdvApi32.TOKEN_TYPE.TokenPrimary, out hPrimaryToken))
            {
                return null;
            }
            // Start the target process with the new token.
            var si = new STARTUPINFO();
            var pi = new PROCESS_INFORMATION();
            if (!CreateProcessWithTokenW(hPrimaryToken, 0, fileName, args, 0, IntPtr.Zero, workingDirectory, ref si, out pi))
            {
                return null;
            }
            return Process.GetProcessById(pi.dwProcessId);
        }
        finally
        {
            hShellProcessToken?.Close();
            hPrimaryToken?.Close();
            hShellProcess?.Close();
        }

    }

    private static Encoding GetEncoding()
    {
        var info = Encoding.GetEncodings().FirstOrDefault(x => x.CodePage == CultureInfo.CurrentCulture.TextInfo.OEMCodePage);
        return info?.GetEncoding();
    }
}
