namespace ContainerDesktop.DesiredStateConfiguration;

using System.Runtime.Serialization;

[Serializable]
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

    protected ResourceException(SerializationInfo info, StreamingContext context) : base(info, context)
    {
    }
}