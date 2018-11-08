using System;
using System.Runtime.Serialization;

namespace CalendarService
{
    [Serializable]
    internal class InvalidEventException : Exception
    {
        public InvalidEventException()
        {
        }

        public InvalidEventException(string message) : base(message)
        {
        }

        public InvalidEventException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected InvalidEventException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}