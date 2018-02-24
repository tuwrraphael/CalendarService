using System;
using System.Runtime.Serialization;

namespace CalendarService
{
    [Serializable]
    internal class AuthorizationStateNotFoundException : Exception
    {
        public AuthorizationStateNotFoundException()
        {
        }

        public AuthorizationStateNotFoundException(string message) : base(message)
        {
        }

        public AuthorizationStateNotFoundException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected AuthorizationStateNotFoundException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}