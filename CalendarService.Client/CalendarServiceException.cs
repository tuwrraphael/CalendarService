using System;
using System.Runtime.Serialization;

namespace CalendarService.Client
{
    [Serializable]
    public class CalendarServiceException : Exception
    {
        public CalendarServiceException()
        {
        }

        public CalendarServiceException(string message) : base(message)
        {
        }

        public CalendarServiceException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected CalendarServiceException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}