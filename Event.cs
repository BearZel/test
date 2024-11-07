using System;

namespace AbakConfigurator.Soe
{
    public class Event
    {
        public DateTime Timestamp { get; set; }
        public string Source { get; set; }
        public string Message { get; set; }

        public Event(DateTime timestamp, string source, string message)
        {
            Timestamp = timestamp;
            Source = source;
            Message = message;
        }
    }
}
