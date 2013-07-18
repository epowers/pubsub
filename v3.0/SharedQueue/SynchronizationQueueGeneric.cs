using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Diagnostics;

namespace Microsoft.WebSolutionsPlatform.Common
{
    /// <summary>
    /// A wrapper class of the Queue class to make the Queue class thread safe.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class SynchronizationQueueGeneric<T> : IDisposable
    {
        internal class QueueObject
        {
            internal T item;
            internal int size;

            internal QueueObject(T item, int size)
            {
                this.item = item;
                this.size = size;
            }
        }

        private bool disposed;

        private long maxQueueSize = 1000000000;
        private long lastErrorLogTick = 0;
        private long numEventsLost = 0;

        private Queue<QueueObject> queue;

        private PerformanceCounter performanceCounter;

        private Guid eventType = Guid.Empty;

        private Mutex mut = new Mutex(false);

        private ManualResetEvent resetEvent = new ManualResetEvent(false);

        private int timeout = 10000;
        /// <summary>
        /// Timeout for blocking calls, default is 10000
        /// </summary>
        public int Timeout
        {
            get
            {
                return timeout;
            }
            internal set
            {
                timeout = value;
            }
        }

        private long size = 0;
        /// <summary>
        /// Size in bytes of queue
        /// </summary>
        public long Size
        {
            get
            {
                return size;
            }
            internal set
            {
                size = value;
            }
        }

        private bool inUse = true;
        /// <summary>
        /// Specifies if the queue object is or can be garbage collected
        /// </summary>
        public bool InUse
        {
            get
            {
                return inUse;
            }
            internal set
            {
                inUse = value;
            }
        }

        private long lastUsedTick = DateTime.Now.Ticks;
        /// <summary>
        /// Specifies when the queue object quit being in use
        /// </summary>
        public long LastUsedTick
        {
            get
            {
                return lastUsedTick;
            }
            internal set
            {
                lastUsedTick = value;
            }
        }

        /// <summary>
        /// Adds an object to the end of the Generic Queue
        /// </summary>
        /// <param name="item">The object to add to the queue</param>
        /// <returns>True if item was successfully queued else False</returns>
        public bool Enqueue(T item)
        {
            return Enqueue(item, 0);
        }

        /// <summary>
        /// Adds an object to the end of the Generic Queue
        /// </summary>
        /// <param name="item">The object to add to the queue</param>
        /// <param name="size">The size in bytes of the item</param>
        /// <returns>True if item was successfully queued else False</returns>
        public bool Enqueue(T item, int size)
        {
            return Enqueue(item, size, timeout);
        }

        /// <summary>
        /// Adds an object to the end of the Generic Queue
        /// </summary>
        /// <param name="item">The object to add to the queue</param>
        /// <param name="size">The size in bytes of the item</param>
        /// <param name="timeoutIn">Timeout in milliseconds for call</param>
        /// <returns>True if item was successfully queued else False</returns>
        public bool Enqueue(T item, int size, int timeoutIn)
        {
            if (this.size > maxQueueSize)
            {
                numEventsLost++;

                if ((DateTime.Now.Ticks - lastErrorLogTick) > 600000000L || numEventsLost % 50000L == 1L)
                {
                    EventLog.WriteEntry("WspEventRouter", "Event queue for application with PID " + Process.GetCurrentProcess().Id.ToString() +
                    " and event type " + eventType.ToString() + " has exceeded max queue size of " + maxQueueSize.ToString() +
                    " bytes and is now losing events until queue size is below max. Total events losts to this point is " + numEventsLost.ToString(),
                    EventLogEntryType.Error);

                    lastErrorLogTick = DateTime.Now.Ticks;
                }

                return false;
            }

            if (mut.WaitOne(timeoutIn, false) == true)
            {
                try
                {
                    QueueObject queueObject = new QueueObject(item, size);

                    queue.Enqueue(queueObject);

                    resetEvent.Set();

                    Interlocked.Add(ref this.size, (long)size);

                    if (performanceCounter != null)
                        performanceCounter.RawValue = (long)queue.Count;
                }
                finally
                {
                    mut.ReleaseMutex();
                }
            }
            else
            {
                EventLog.WriteEntry("WspEventRouter", "Event enqueue timed out for application with PID " + Process.GetCurrentProcess().Id.ToString() +
                " and event type " + eventType.ToString() + " with the event being lost",
                EventLogEntryType.Error);

                return false;
            }

            return true;
        }

        /// <summary>
        /// Removes an object from the Generic Queue
        /// </summary>
        public T Dequeue()
        {
            return Dequeue(timeout);
        }

        /// <summary>
        /// Removes an object from the Generic Queue
        /// </summary>
        /// <param name="timeoutIn">Timeout in milliseconds for call</param>
        public T Dequeue(int timeoutIn)
        {
            QueueObject item;

            if (queue.Count > 0 || resetEvent.WaitOne(timeoutIn, false) == true)
            {
                if (mut.WaitOne(timeoutIn, false) == true)
                {
                    try
                    {
                        if (queue.Count > 0)
                        {
                            item = queue.Dequeue();

                            Interlocked.Add(ref this.size, -((long)item.size));
                        }
                        else
                        {
                            resetEvent.Reset();
                            mut.ReleaseMutex();

                            return default(T);
                        }

                        if (performanceCounter != null)
                            performanceCounter.RawValue = (long)queue.Count;

                        if (queue.Count > 0)
                        {
                            resetEvent.Set();
                        }
                        else
                        {
                            resetEvent.Reset();
                        }
                    }
                    catch (InvalidOperationException)
                    {
                        resetEvent.Reset();
                        mut.ReleaseMutex();

                        return default(T);
                    }

                    mut.ReleaseMutex();

                    return item.item;
                }
                else
                {
                    return default(T);
                }
            }

            else
            {
                return default(T);
            }
        }

        /// <summary>
        /// Default constructor
        /// </summary>
        public SynchronizationQueueGeneric()
        {
            queue = new Queue<QueueObject>();
        }

        /// <summary>
        /// Default constructor
        /// </summary>
        /// <param name="performanceCounter">Performance counter showing size of queue.</param>
        /// <param name="eventType">The event type being queued</param>
        public SynchronizationQueueGeneric(PerformanceCounter performanceCounter, Guid eventType)
            : this()
        {
            this.performanceCounter = performanceCounter;
            this.performanceCounter.RawValue = 0;
            this.eventType = eventType;
        }

        /// <summary>
        /// Default constructor
        /// </summary>
        /// <param name="capacity">The initial number of elements that the Generic Queue can contain</param>
        public SynchronizationQueueGeneric(int capacity)
        {
            queue = new Queue<QueueObject>(capacity);
        }

        /// <summary>
        /// Default constructor
        /// </summary>
        /// <param name="capacity">The initial number of elements that the Generic Queue can contain</param>
        /// <param name="performanceCounter">Performance counter showing size of queue.</param>
        /// <param name="eventType">The event type being queued</param>
        public SynchronizationQueueGeneric(int capacity, PerformanceCounter performanceCounter, Guid eventType)
            : this(capacity)
        {
            this.performanceCounter = performanceCounter;
            this.performanceCounter.RawValue = 0;
            this.eventType = eventType;
        }

        /// <summary>
        /// Disposes of the object.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
        }

        /// <summary>
        /// Disposes of the object.
        /// </summary>
        /// <param name="disposing">True if disposing.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (mut != null)
                {
                    mut.Close();
                    mut = null;

                    resetEvent.Close();
                    resetEvent = null;

                    if (performanceCounter != null)
                    {
                        try
                        {
                            performanceCounter.RawValue = 0;
                        }
                        catch
                        {
                        }
                    }
                }

                disposed = true;

                if (disposing)
                {
                    GC.SuppressFinalize(this);
                }
            }
        }

        /// <summary>
        /// Destructor
        /// </summary>
        ~SynchronizationQueueGeneric()
        {
            Dispose(false);
        }
    }
}
