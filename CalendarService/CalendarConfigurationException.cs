using System;
using System.Runtime.Serialization;

namespace CalendarService
{
    [Serializable]
    internal class CalendarConfigurationException : Exception
    {
        public CalendarConfigurationException()
        {
        }

        public CalendarConfigurationException(string message) : base(message)
        {
        }

        public CalendarConfigurationException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected CalendarConfigurationException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}