using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace TomSSL.Utilities.Queuing
{
    public static class ActionQueue<T>
    {
        private static BlockingCollection<Timestamped<T>> queue;
        private static CancellationTokenSource cancellationTokenSource;
        private static ConcurrentQueue<Timestamped<T>> internalQueueUsedOnlyForTryPeekToGetLatency;

        static ActionQueue()
        {
            Clear();
        }

        public static int Count
        {
            get
            {
                return internalQueueUsedOnlyForTryPeekToGetLatency.Count;
            }
        }

        public static void Clear()
        {
            internalQueueUsedOnlyForTryPeekToGetLatency = new ConcurrentQueue<Timestamped<T>>();
            queue = new BlockingCollection<Timestamped<T>>(internalQueueUsedOnlyForTryPeekToGetLatency);
            cancellationTokenSource = new CancellationTokenSource();
        }

        public static T Dequeue()
        {
            Timestamped<T> timestampedItem;
            queue.TryTake(out timestampedItem);
            return timestampedItem.Item;
        }

        public static T Dequeue(TimeSpan timeout)
        {
            Timestamped<T> timestampedItem;
            return queue.TryTake(out timestampedItem, timeout) ? timestampedItem.Item : default(T);
        }

        public static void Enqueue(T item)
        {
            var timestampedItem = new Timestamped<T>(item);
            queue.Add(timestampedItem);
        }

        public static async Task<T> Consume(Action<T> action)
        {
            return await Task.Run(() =>
            {
                try
                {
                    foreach (var item in queue.GetConsumingEnumerable(cancellationTokenSource.Token).AsParallel().AsOrdered())
                    {
                        action(item.Item);
                    }
                }
                catch (OperationCanceledException)
                {
                    return default(T);
                }

                return default(T);
            });
        }

        public static async Task<Timestamped<T>> Consume(Action<Timestamped<T>> action)
        {
            return await Task.Run(() =>
            {
                try
                {
                    foreach (var item in queue.GetConsumingEnumerable(cancellationTokenSource.Token).AsParallel().AsOrdered())
                    {
                        action(item);
                    }
                }
                catch (OperationCanceledException)
                {
                    return default(Timestamped<T>);
                }

                return default(Timestamped<T>);
            });
        }

        public static async void Cancel()
        {
            await Task.Run(() => cancellationTokenSource.Cancel());
        }

        public static async void Cancel(int delayInMilliseconds)
        {
            await Task.Run(() =>
            {
                Thread.Sleep(delayInMilliseconds);
                Cancel();
            });
        }

        public static long GetLatencyInNanoseconds()
        {
            Timestamped<T> timestampedItem;
            if (internalQueueUsedOnlyForTryPeekToGetLatency.TryPeek(out timestampedItem))
            {
                return 100 * (DateTimeOffset.Now - timestampedItem.TimeStamp).Ticks;
            }

            return 0;
        }

        public static long GetLatencyInMilliseconds()
        {
            return GetLatencyInNanoseconds() / 1000000;
        }

        public static long GetLatencyInSeconds()
        {
            return GetLatencyInMilliseconds() / 1000;
        }
    }
}
