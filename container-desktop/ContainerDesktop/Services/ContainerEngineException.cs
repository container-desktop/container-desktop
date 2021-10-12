namespace ContainerDesktop.Services;

using System.Runtime.Serialization;

[Serializable]
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

    protected ContainerEngineException(SerializationInfo info, StreamingContext context) : base(info, context)
    {
    }
}