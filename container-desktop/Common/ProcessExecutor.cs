namespace ContainerDesktop.Common;

using System.Diagnostics;

public class ProcessExecutor : IProcessExecutor
{
    public Task<int> ExecuteAsync(
        string command,
        string args,
        string workingDir = null,
        Action<string> stdOut = null,
        Action<string> stdErr = null,
        params (string key, string value)[] environmentVariables)
    {
        return ProcessExtensions.StartProcess(command, args, workingDir, stdOut, stdErr, environmentVariables)
            .CompleteAsync();
    }

    public int Execute(
        string command,
        string args,
        string workingDir = null,
        Action<string> stdOut = null,
        Action<string> stdErr = null,
        params (string key, string value)[] environmentVariables)
    {
        return ProcessExtensions.StartProcess(command, args, workingDir, stdOut, stdErr, environmentVariables)
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
        return ProcessExtensions.StartProcess(command, args, workingDir, stdOut, stdErr, environmentVariables);
    }
}
