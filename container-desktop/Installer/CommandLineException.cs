namespace ContainerDesktop.Installer;

public class CommandLineException : Exception
{
    public CommandLineException()
    {
    }

    public CommandLineException(string message) : base(message)
    {
    }

    public CommandLineException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
