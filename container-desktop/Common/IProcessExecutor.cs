namespace ContainerDesktop.Common;

using System.Diagnostics;

/// <summary>
/// Represents a process.
/// </summary>
public interface IProcessExecutor
{
    /// <summary>
    /// Executes the given command.
    /// </summary>
    /// <param name="command">The command to execute.</param>
    /// <param name="args">The command arguments.</param>
    /// <param name="workingDir">The working directory.</param>
    /// <param name="stdOut">An action to catch std out.</param>
    /// <param name="stdErr">An action to catch std err.</param>
    /// <param name="environmentVariables">Environment variables to pass to the process.</param>
    /// <returns>The exit code.</returns>
    Task<int> ExecuteAsync(
        string command,
        string args,
        string workingDir = null,
        Action<string> stdOut = null,
        Action<string> stdErr = null,
        params (string key, string value)[] environmentVariables);

    /// <summary>
    /// Executes the given command.
    /// </summary>
    /// <param name="command">The command to execute.</param>
    /// <param name="args">The command arguments.</param>
    /// <param name="workingDir">The working directory.</param>
    /// <param name="stdOut">An action to catch std out.</param>
    /// <param name="stdErr">An action to catch std err.</param>
    /// <param name="environmentVariables">Environment variables to pass to the process.</param>
    /// <returns>The exit code.</returns>
    int Execute(
        string command,
        string args,
        string workingDir = null,
        Action<string> stdOut = null,
        Action<string> stdErr = null,
        params (string key, string value)[] environmentVariables);

    /// <summary>
    /// Starts the given command.
    /// </summary>
    /// <param name="command">The command to execute.</param>
    /// <param name="args">The command arguments.</param>
    /// <param name="workingDir">The working directory.</param>
    /// <param name="stdOut">An action to catch std out.</param>
    /// <param name="stdErr">An action to catch std err.</param>
    /// <param name="environmentVariables">Environment variables to pass to the process.</param>
    /// <returns>The <see cref="Process"/>.</returns>
    Process Start(
        string command,
        string args,
        string workingDir = null,
        Action<string> stdOut = null,
        Action<string> stdErr = null,
        params (string key, string value)[] environmentVariables);

    /// <summary>
    /// Starts the given command.
    /// </summary>
    /// <param name="command">The command to execute.</param>
    /// <param name="args">The command arguments.</param>
    /// <param name="useShellExecute">Use shell execute.</param>
    /// <param name="workingDir">The working directory.</param>
    /// <param name="environmentVariables">Environment variables to pass to the process.</param>
    /// <returns>The <see cref="Process"/>.</returns>
    Process Start(
        string command,
        string args,
        bool useShellExecute = false,
        string workingDir = null,
        params (string key, string value)[] environmentVariables);

    /// <summary>
    /// Starts the given command.
    /// </summary>
    /// <param name="command">The command to execute.</param>
    /// <param name="args">The command arguments.</param>
    /// <returns>The <see cref="Process"/>.</returns>
    Process Start(string command, string args);
}
