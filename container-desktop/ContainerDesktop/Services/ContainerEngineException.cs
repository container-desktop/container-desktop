namespace ContainerDesktop.Services;

public class ContainerEngineException : Exception
{
    public ContainerEngineException()
    {
    }

    public ContainerEngineException(string message) : base(message)
    {
    }

    public ContainerEngineException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
