namespace ContainerDesktop.DesiredStateConfiguration;

public class ResourceException : Exception
{
    public ResourceException()
    {
    }

    public ResourceException(string message) : base(message)
    {
    }

    public ResourceException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
