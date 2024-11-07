using System;
using System.Runtime.Serialization;

namespace AbakConfigurator.Secure
{
    [Serializable]
    public class SecureProviderException : Exception
    {
        public SecureProviderException(string message) : base(message)
        {

        }

        protected SecureProviderException(SerializationInfo info, StreamingContext context) : base(info, context)
        { 

        }
    }
}
