using System;
using System.Runtime.Serialization;

namespace CalendarService
{
    [Serializable]
    internal class ReminderDeliveryException : Exception
    {
        public ReminderDeliveryException()
        {
        }

        public ReminderDeliveryException(string message) : base(message)
        {
        }

        public ReminderDeliveryException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected ReminderDeliveryException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}