using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace TomSSL.Utilities.Queuing.Tests
{
    [TestClass]
    public class ActionQueueTests
    {
        [TestMethod]
        public void CanProcessQueue()
        {
            var total = 0;
            ActionQueue<int>.Clear();

            // Start processing queue
            var task = ActionQueue<int>.Consume(x => total += x);

            // Arrange to stop processing the queue in 0.1 sec
            ActionQueue<int>.Cancel(100);

            // Add items to queue (this is almost instantaneous).
            Enumerable.Range(1, 10).ToList().ForEach(ActionQueue<int>.Enqueue);

            // Wait 0.1 sec for the cancellation to take effect.
            Thread.Sleep(100);
            Assert.AreEqual(55, total);
        }

        [TestMethod]
        public void CanDequeueManually()
        {
            var random = new Random();
            var max = 1000;
            var dequeuedCount = random.Next(1, max - 1);
            var value = 0;
            var remainder = 0;
            ActionQueue<int>.Clear();

            // Add random number of items to queue.
            Enumerable.Range(1, max).ToList().ForEach(ActionQueue<int>.Enqueue);

            // Dequeue a (different and smaller) random number of times.
            for (var i = 0; i < dequeuedCount; i++)
            {
                value = ActionQueue<int>.Dequeue();
            }

            // Work out how many items there should be left in the queue.
            remainder = max - dequeuedCount;

            // Make sure we dequeued as many items as expected.
            Assert.AreEqual(value, dequeuedCount);
            // Make sure there are as many items left in the queue as expected.
            Assert.AreEqual(ActionQueue<int>.Count, remainder);
        }

        [TestMethod]
        public void CanCancel()
        {
            var arr = new List<int>();
            ActionQueue<int>.Clear();

            // Start processing queue at the rate of one item every 11ms.
            var task = ActionQueue<int>.Consume(x =>
            {
                arr.Add(x);
                Thread.Sleep(11);
            });

            // Arrange to stop processing the queue in 0.1 sec
            ActionQueue<int>.Cancel(95);

            // Add ten items to queue.
            Enumerable.Range(0, 10).ToList().ForEach(ActionQueue<int>.Enqueue);

            // Wait 0.1 sec.
            Thread.Sleep(100);

            // Add another hundred items. The consumer should be cancelled before this happens.
            Enumerable.Range(10, 100).ToList().ForEach(ActionQueue<int>.Enqueue);

            // Wait 0.1 sec for the extra items to be processed in the event of the cancellation not working.
            Thread.Sleep(100);
            Assert.AreEqual(9, arr.Count);
        }

        [TestMethod]
        public void CanCancelWithTimestampedConsume()
        {
            var arr = new List<Timestamped<int>>();
            ActionQueue<int>.Clear();

            // Start processing queue at the rate of one item every 11ms.
            var t = ActionQueue<int>.Consume(x =>
            {
                arr.Add(x);
                Thread.Sleep(11);
            });

            // Arrange to stop processing the queue in 0.1 sec
            ActionQueue<int>.Cancel(95);

            // Add ten items.
            Enumerable.Range(0, 10).ToList().ForEach(ActionQueue<int>.Enqueue);

            // Wait 0.1 sec.
            Thread.Sleep(100);

            // Add another hundred items. The consumer should be cancelled before this happens.
            Enumerable.Range(10, 100).ToList().ForEach(ActionQueue<int>.Enqueue);

            // Wait 0.1 sec for the extra items to be processed in the event of the cancellation not working.
            Thread.Sleep(100);
            Assert.AreEqual(9, arr.Count);
        }

        [TestMethod]
        public void CanGetLatency()
        {
            var arr = new List<int>();
            ActionQueue<int>.Clear();

            // Start processing queue at the rate of one item every 11ms.
            var t = ActionQueue<int>.Consume(x =>
            {
                arr.Add(x);
                Thread.Sleep(11);
            });

            // Arrange to stop processing the queue in 0.1 sec
            ActionQueue<int>.Cancel(95);

            // Add ten items.
            Enumerable.Range(0, 10).ToList().ForEach(ActionQueue<int>.Enqueue);

            // Wait 0.1 sec.
            Thread.Sleep(100);

            var latency = ActionQueue<int>.GetLatencyInMilliseconds();

            // Wait 0.1 sec for the extra items to be processed in the event of the cancellation not working.
            Thread.Sleep(100);
            Assert.IsTrue(latency >= 100, string.Format("Latency was meant to be >= {0}ms but it was {1}ms.", 100, latency));
        }
    }
}
