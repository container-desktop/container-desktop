using System;
using System.Runtime.Serialization;

namespace ContainerDesktop.Services
{
    [Serializable]
    internal class BootstrapException : Exception
    {
        public BootstrapException()
        {
        }

        public BootstrapException(string message) : base(message)
        {
        }

        public BootstrapException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected BootstrapException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}