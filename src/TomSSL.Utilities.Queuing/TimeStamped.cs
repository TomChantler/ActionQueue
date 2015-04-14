using System;

namespace TomSSL.Utilities.Queuing
{
    public class Timestamped<T>
    {
        public Timestamped(T item)
        {
            this.Item = item;
            this.TimeStamp = DateTime.UtcNow;
        }

        public T Item { get; private set; }

        public DateTimeOffset TimeStamp { get; private set; }
    }
}
