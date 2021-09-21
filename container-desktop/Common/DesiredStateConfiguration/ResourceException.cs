using System;
using System.Runtime.Serialization;

namespace ContainerDesktop.Common.DesiredStateConfiguration
{
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
}