using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Threading;
using System.Diagnostics;
using System.Resources;
using System.Reflection;
using System.Runtime.Serialization;
using Microsoft.WebSolutionsPlatform.Common;

[assembly: CLSCompliant(true)]

namespace Microsoft.WebSolutionsPlatform.PubSubManager
{
    /// <summary>
    /// The ReturnCode enum defines the return codes when publishing events
    /// </summary>
    public enum ReturnCode : int
    {
        /// <summary>
        /// Action was successful
        /// </summary>
        Success = 0,
        /// <summary>
        /// Action timed out
        /// </summary>
        Timeout = -1,
        /// <summary>
        /// Queue does not exist. Check to see if WspEventRouter is running.
        /// </summary>
        QueueNameDoesNotExist = 2,
        /// <summary>
        /// Invalid argument was passed
        /// </summary>
        InvalidArgument = 3,
        /// <summary>
        /// There was not sufficient space to publish the event. This indicates the queue is full.
        /// </summary>
        InsufficientSpace = 5,
        /// <summary>
        /// There is not sufficient memory to processing the event
        /// </summary>
        InsufficientMemory = 1455,
        /// <summary>
        /// The event overflowed the buffer
        /// </summary>
        Overflow = 9999
    }

    /// <summary>
    /// This class is used to publish Wsp events using Rx
    /// </summary>
    public class WspEventPublish : IObserver<WspEvent>
    {
        private static object lockObj = new object();
        private static PublishManager pubMgr = null;
        private static long expectedMaxLatency;  // This is in ticks

        /// <summary>
        /// Constructor
        /// </summary>
        public WspEventPublish()
        {
            if (pubMgr == null)
            {
                lock (lockObj)
                {
                    if (pubMgr == null)
                    {
                        pubMgr = new PublishManager();

                        expectedMaxLatency = (long)((pubMgr.RetryAttempts * pubMgr.Timeout) + (pubMgr.RetryAttempts * pubMgr.RetryPause)) * 10000L;
                    }
                }
            }
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="timeout">Timeout for publishing an event</param>
        [CLSCompliant(false)]
        public WspEventPublish(uint timeout)
        {
            if (pubMgr == null)
            {
                lock (lockObj)
                {
                    if (pubMgr == null)
                    {
                        pubMgr = new PublishManager(timeout);

                        expectedMaxLatency = (long)((pubMgr.RetryAttempts * pubMgr.Timeout) + (pubMgr.RetryAttempts * pubMgr.RetryPause)) * 10000L;
                    }
                }
            }
        }

        /// <summary>
        /// The OnCompleted is not used for publishing events
        /// </summary>
        public void OnCompleted()
        {
        }

        /// <summary>
        /// The OnError is not used for publishing events
        /// </summary>
        /// <param name="error">Error exception</param>
        public void OnError(Exception error)
        {
        }

        /// <summary>
        /// The OnNext is used to publish events using Rx
        /// </summary>
        /// <param name="wspEvent">The WspEvent being published</param>
        /// <param name="rc">Return code</param>
        public void OnNext(WspEvent wspEvent, out ReturnCode rc)
        {
            ThreadPriority threadPriority;
            WspEvent[] wspEvents = null;
            long beginTick = DateTime.Now.Ticks;
            long interceptorTicks;
            long totalTicks;

            rc = ReturnCode.Success;

            threadPriority = Thread.CurrentThread.Priority;
            Thread.CurrentThread.Priority = ThreadPriority.Lowest;

            if (Interceptor.publishInterceptor != null)
            {
                try
                {
                    if (Interceptor.publishInterceptor(wspEvent, null, out wspEvents) == false)
                    {
                        return;
                    }
                }
                catch
                {
                }
            }

            interceptorTicks = DateTime.Now.Ticks - beginTick;

            if (wspEvents == null || wspEvents.Length == 0)
            {
                pubMgr.Publish(wspEvent.SerializedEvent, out rc);
            }
            else
            {
                for (int i = 0; i < wspEvents.Length; i++)
                {
                    pubMgr.Publish(wspEvents[i].SerializedEvent, out rc);
                }
            }

            totalTicks = DateTime.Now.Ticks - beginTick;

            if (totalTicks > expectedMaxLatency)
            {
                StackTrace st = new StackTrace(true);
                StringBuilder sb = new StringBuilder();
                sb.Append("\nEventType = ");
                sb.Append(wspEvent.EventType.ToString());
                sb.Append("\nProcessID = ");
                sb.Append(Process.GetCurrentProcess().Id.ToString());
                sb.Append("\nManagedThreadID = ");
                sb.Append(Thread.CurrentThread.ManagedThreadId.ToString());
                sb.Append("\nAppDomain = ");
                sb.Append(AppDomain.CurrentDomain.ApplicationIdentity.FullName);
                sb.Append("\nStackTrace = ");
                sb.Append(st.ToString());

                EventLog.WriteEntry("WspEventRouter", "Event publish time threshold exceeded: " + sb.ToString(), EventLogEntryType.Warning);
            }

            Thread.CurrentThread.Priority = threadPriority;
        }

        /// <summary>
        /// The OnNext is used to publish events using Rx
        /// </summary>
        /// <param name="wspEvent">The WspEvent being published</param>
        public void OnNext(WspEvent wspEvent)
        {
            ThreadPriority threadPriority;
            WspEvent[] wspEvents = null;
            long beginTick = DateTime.Now.Ticks;
            long interceptorTicks;
            long totalTicks;

            threadPriority = Thread.CurrentThread.Priority;
            Thread.CurrentThread.Priority = ThreadPriority.Lowest;

            if (Interceptor.publishInterceptor != null)
            {
                try
                {
                    if (Interceptor.publishInterceptor(wspEvent, null, out wspEvents) == false)
                    {
                        return;
                    }
                }
                catch
                {
                }
            }

            interceptorTicks = DateTime.Now.Ticks - beginTick;

            if (wspEvents == null || wspEvents.Length == 0)
            {
                pubMgr.Publish(wspEvent.SerializedEvent);
            }
            else
            {
                for (int i = 0; i < wspEvents.Length; i++)
                {
                    pubMgr.Publish(wspEvents[i].SerializedEvent);
                }
            }

            totalTicks = DateTime.Now.Ticks - beginTick;

            if (totalTicks > expectedMaxLatency)
            {
                StackTrace st = new StackTrace(true);
                StringBuilder sb = new StringBuilder();
                sb.Append("\nEventType = ");
                sb.Append(wspEvent.EventType.ToString());
                sb.Append("\nProcessID = ");
                sb.Append(Process.GetCurrentProcess().Id.ToString());
                sb.Append("\nManagedThreadID = ");
                sb.Append(Thread.CurrentThread.ManagedThreadId.ToString());
                sb.Append("\nAppDomain = ");
                sb.Append(AppDomain.CurrentDomain.ApplicationIdentity.FullName);
                sb.Append("\nStackTrace = ");
                sb.Append(st.ToString());

                EventLog.WriteEntry("WspEventRouter", "Event publish threshold exceeded for: " + sb.ToString(), EventLogEntryType.Warning);
            }

            Thread.CurrentThread.Priority = threadPriority;
        }

        /// <summary>
        /// The OnNextPrivate is used to publish events using Rx but bypassing the interceptor
        /// </summary>
        /// <param name="wspEvent">The WspEvent being published</param>
        internal void OnNextPrivate(WspEvent wspEvent)
        {
            pubMgr.Publish(wspEvent.SerializedEvent);
        }
    }

    /// <summary>
    /// PublishManager is used by applcations to publish events.
    /// </summary>
    internal class PublishManager : IDisposable
    {
        private static UInt32 defaultEventTimeout = 10000;

        private string eventQueueName = @"WspEventQueue";
        private SharedQueue eventQueue;

        private bool disposed = false;

        private UInt32 retryAttempts;
        /// <summary>
        /// Number of times to retry a failed enqueue request before returning a fail to the application
        /// </summary>
        internal UInt32 RetryAttempts
        {
            get
            {
                return retryAttempts;
            }
            set
            {
                retryAttempts = value;
            }
        }

        private UInt32 retryPause;
        /// <summary>
        /// Number of milliseconds to wait before retrying an enqueue request
        /// </summary>
        internal UInt32 RetryPause
        {
            get
            {
                return retryPause;
            }
            set
            {
                retryPause = value;
            }
        }

        private UInt32 timeout;
        /// <summary>
        /// Number of milliseconds of timeout for publishing events
        /// </summary>
        internal UInt32 Timeout
        {
            get
            {
                return timeout;
            }
            set
            {
                timeout = value;
            }
        }

        /// <summary>
        /// Size in bytes of the SharedQueue
        /// </summary>
        internal UInt32 QueueSize
        {
            get
            {
                if (eventQueue == null)
                {
                    return 0;
                }

                return eventQueue.QueueSize;
            }
        }

        /// <summary>
        /// Constructor
        /// </summary>
        internal PublishManager()
            : this(defaultEventTimeout)
        {
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="timeout">Timeout for publishing an event</param>
        internal PublishManager(UInt32 timeout)
        {
            this.timeout = timeout;
            this.retryAttempts = 3u;
            this.retryPause = 1000u;

            try
            {
                eventQueue = new SharedQueue(eventQueueName, 100);

                if (eventQueue == null)
                {
                    throw new PubSubConnectionFailedException("Connection to the Event System failed");
                }
            }
            catch (SharedQueueDoesNotExistException e)
            {
                throw new PubSubQueueDoesNotExistException(e.Message, e.InnerException);
            }
            catch (SharedQueueInsufficientMemoryException e)
            {
                throw new PubSubInsufficientMemoryException(e.Message, e.InnerException);
            }
            catch (SharedQueueInitializationException e)
            {
                throw new PubSubInitializationException(e.Message, e.InnerException);
            }
        }

        /// <summary>
        /// Publishes an event to the event service
        /// </summary>
        /// <param name="serializedEvent">A serialized WspEvent object</param>
        internal void Publish(byte[] serializedEvent)
        {
            ReturnCode rc;

            Publish(serializedEvent, out rc);

            if (rc == ReturnCode.Success)
            {
                return;
            }

            if (rc == ReturnCode.Timeout)
            {
                throw new TimeoutException("Enqueue timed out");
            }

            if (rc == ReturnCode.InsufficientSpace)
            {
                SharedQueueFullException e = new SharedQueueFullException("Queue is full");

                throw new PubSubQueueFullException(e.Message, e.InnerException);
            }

            if (rc != ReturnCode.Success)
            {
                SharedQueueException e = new SharedQueueException("(HRESULT:" + rc.ToString() + ") Enqueue failed");

                throw new PubSubException(e.Message, e.InnerException);
            }
        }

        /// <summary>
        /// Publishes an event to the event service
        /// </summary>
        /// <param name="serializedEvent">A serialized WspEvent object</param>
        /// <param name="rc">Return code</param>
        internal void Publish(byte[] serializedEvent, out ReturnCode rc)
        {
            Common.ReturnCode rcCommon;

            rc = ReturnCode.InvalidArgument;

            for (int tries = 0; tries <= retryAttempts; tries++)
            {
                eventQueue.Enqueue(serializedEvent, out rcCommon, timeout);

                rc = (ReturnCode)rcCommon;

                if (rc != ReturnCode.Timeout)
                {
                    return;
                }

                Thread.Sleep((int)retryPause);
            }
        }

        /// <summary>
        /// Dispose the object
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
        }

        /// <summary>
        /// Dispose the object
        /// </summary>
        /// <param name="disposing">True if disposing</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                try
                {
                    if (eventQueue != null)
                    {
                        eventQueue.Dispose();
                    }
                }
                finally
                {
                    disposed = true;

                    if (disposing)
                    {
                        GC.SuppressFinalize(this);
                    }
                }
            }
        }

        /// <summary>
        /// Destructor
        /// </summary>
        ~PublishManager()
        {
            Dispose(false);
        }
    }
}
