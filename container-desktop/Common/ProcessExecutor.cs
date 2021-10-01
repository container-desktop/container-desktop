namespace ContainerDesktop.Common;

using System.Diagnostics;
using System.Threading;

public class ProcessExecutor : IProcessExecutor
{
    public Task<int> ExecuteAsync(
        string command,
        string args,
        string workingDir = null,
        Action<string> stdOut = null,
        Action<string> stdErr = null,
        CancellationToken cancellationToken = default,
        params (string key, string value)[] environmentVariables)
    {
        return ProcessExtensions.StartProcess(command, args, workingDir, false, stdOut, stdErr, environmentVariables)
            .CompleteAsync(cancellationToken);
    }

    public int Execute(
        string command,
        string args,
        string workingDir = null,
        Action<string> stdOut = null,
        Action<string> stdErr = null,
        params (string key, string value)[] environmentVariables)
    {
        return ProcessExtensions.StartProcess(command, args, workingDir, false, stdOut, stdErr, environmentVariables)
            .Complete();
    }

    public Process Start(
        string command,
        string args,
        string workingDir = null,
        Action<string> stdOut = null,
        Action<string> stdErr = null,
        params (string key, string value)[] environmentVariables)
    {
        return ProcessExtensions.StartProcess(command, args, workingDir, false, stdOut, stdErr, environmentVariables);
    }

    public Process Start(
        string command,
        string args,
        bool useShellExecute = false,
        string workingDir = null,
        params (string key, string value)[] environmentVariables)
    {
        return ProcessExtensions.StartProcess(command, args, workingDir, useShellExecute, null, null, environmentVariables);
    }

    public Process Start(string command, string args)
    {
        return ProcessExtensions.StartProcess(command, args, null, false, null, null);
    }
}
