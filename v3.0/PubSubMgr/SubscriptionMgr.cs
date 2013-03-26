using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Net;
using System.Resources;
using System.Reflection;
using Microsoft.WebSolutionsPlatform.Common;

namespace Microsoft.WebSolutionsPlatform.PubSubManager
{
    /// <summary>
    /// The ISubscriptionCallback interface is implemented by an event subscriber class. The
    /// SubscriptionCallback method is then called to deliver events to the application.
    /// </summary>
    public interface ISubscriptionCallback
    {
        /// <summary>
        /// This method is passed to the SubscriptionManager as the callback for delivering events.
        /// </summary>
        /// <param name="eventType">Event type for the event being passed.</param>
        /// <param name="wspEvent">The serialized version of the event.</param>
        void SubscriptionCallback(Guid eventType, WspEvent wspEvent);
    }

    internal struct StateInfo
    {
        public object callBack;
        public Guid eventType;
        public WspEvent wspEvent;
    }

    /// <summary>
    /// This class is used to subscribe to events using Rx
    /// </summary>
    public class WspEventObservable : IObservable<WspEvent>
    {
        private static Thread subscriptionThread = null;
        private static bool subscriptionThreadReady = false;

        private static object lockObj = new object();

        private static Guid initialEventType;
        private static bool initialLocalOnly;

        /// <summary>
        /// Subscription Manager
        /// </summary>        
        private static SubscriptionManager wspSubscriptionManager = null;

        /// <summary>
        /// The list of observers for wsp event object.
        /// </summary>
        private static Dictionary<Guid, List<IObserver<WspEvent>>> observersByEventType = new Dictionary<Guid, List<IObserver<WspEvent>>>();

        private Guid eventType;
        private bool localOnly;

        /// <summary>
        /// The ObservableSubscription constructor is used to create an observable subscription to then subscribe to via Rx
        /// </summary>
        /// <param name="eventType">The event type to subscribe to</param>
        /// <param name="localOnly">True if only local events are to be subscribed to</param>
        public WspEventObservable(Guid eventType, bool localOnly)
        {
            this.eventType = eventType;
            this.localOnly = localOnly;
        }

        /// <summary>
        /// Subscribe method to provide an observer
        /// </summary>
        /// <param name="observer">The observer to be called for each event</param>
        /// <returns></returns>
        public IDisposable Subscribe(IObserver<WspEvent> observer)
        {
            lock (lockObj)
            {
                if (observersByEventType.Count == 0)
                {
                    initialEventType = eventType;
                    initialLocalOnly = localOnly;

                    subscriptionThread = new Thread(Start);

                    subscriptionThread.Start();

                    while (subscriptionThreadReady == false)
                    {
                        Thread.Sleep(10);
                    }
                }
                else
                {
                    if (observersByEventType.ContainsKey(eventType) == true)
                    {
                        wspSubscriptionManager.RemoveSubscription(eventType);
                    }

                    wspSubscriptionManager.AddSubscription(eventType, localOnly);
                }

                List<IObserver<WspEvent>> observers;

                if (observersByEventType.TryGetValue(eventType, out observers) == true)
                {
                    observers.Add(observer);
                }
                else
                {
                    observers = new List<IObserver<WspEvent>>();
                    observers.Add(observer);
                    observersByEventType[eventType] = observers;
                }
            }

            return new WspSubscriptionDisposable(this, observer);
        }

        class WspSubscriptionDisposable : IDisposable
        {
            private readonly WspEventObservable parent;
            private readonly IObserver<WspEvent> observer;

            public WspSubscriptionDisposable(WspEventObservable parent, IObserver<WspEvent> observer)
            {
                this.parent = parent;
                this.observer = observer;
            }

            public void Dispose()
            {
                parent.Dispose(observer);
            }
        }

        /// <summary>
        /// Starts this instance.
        /// </summary>
        private static void Start()
        {
            try
            {
                var wspSubscriptionCallback = new SubscriptionManager.Callback(SubscriptionCallback);
                wspSubscriptionManager = new SubscriptionManager(wspSubscriptionCallback);
                wspSubscriptionManager.AddSubscription(initialEventType, initialLocalOnly);

                subscriptionThreadReady = true;

                Thread.Sleep(Timeout.Infinite);
            }
            catch (ThreadAbortException)
            {
                subscriptionThreadReady = false;
            }
        }

        /// <summary>
        /// callback subscription
        /// </summary>
        /// <param name="eventType">Type of the event.</param>
        /// <param name="wspEvent">The serialized event.</param>
        internal static void SubscriptionCallback(Guid eventType, WspEvent wspEvent)
        {
            WspEvent[] wspEvents = null;

            List<IObserver<WspEvent>> observers;

            lock(lockObj)
            {
                if (observersByEventType.TryGetValue(eventType, out observers) == true)
                {
                    for (int i = 0; i < observers.Count; i++)
                    {
                        if (Interceptor.subscribeInterceptor != null)
                        {
                            try
                            {
                                if (Interceptor.subscribeInterceptor(wspEvent, out wspEvents) == false)
                                {
                                    return;
                                }
                            }
                            catch
                            {
                            }
                        }

                        if (wspEvents == null || wspEvents.Length == 0)
                        {
                            observers[i].OnNext(wspEvent);
                        }
                        else
                        {
                            for (int j = 0; j < wspEvents.Length; j++)
                            {
                                observers[i].OnNext(wspEvents[j]);
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Dispose for the ObservableSubscription object
        /// </summary>
        /// <param name="observer">Observer to be disposed</param>
        private void Dispose(IObserver<WspEvent> observer)
        {
            lock(lockObj)
            {
                List<IObserver<WspEvent>> observers;

                if (observersByEventType.TryGetValue(eventType, out observers) == true)
                {
                    int last = observers.LastIndexOf(observer);
                    if (last >= 0)
                    {
                        observers.RemoveAt(last);
                    }

                    if (observers.Count == 0)
                    {
                        observersByEventType.Remove(eventType);

                        wspSubscriptionManager.RemoveSubscription(eventType);
                    }
                }

                if (observersByEventType.Count == 0 && subscriptionThread != null)
                {
                    subscriptionThread.Abort();
                    subscriptionThread = null;

                    wspSubscriptionManager = null;
                }

            }
        }
    }

    /// <summary>
    /// This class is used to subscribe to events.
    /// </summary>
    public class SubscriptionManager
    {
        private bool disposed;

        private static UInt32 defaultEventTimeout = 10000;
        private static uint subscriptionRefreshIncrement = 3; // in minutes

        private static PublishManager publishMgr;
        private static int publishMgrRefCount = 0;

        private static object lockObject = new object();

        private string eventQueueName = @"WspEventQueue";
        private SharedQueue eventQueue;

        private Dictionary<Guid, Subscription> subscriptions;

        private Thread listenThread;

        /// <summary>
        /// Defines the callback method for delivering events to an application.
        /// </summary>
        /// <param name="eventType">Event type for the event being passed.</param>
        /// <param name="wspEvent">The WspEvent with Headers and Body.</param>
        public delegate void Callback(Guid eventType, WspEvent wspEvent);

        private bool listenForEvents;
        /// <summary>
        /// Starts and stops listening for events
        /// </summary>
        public bool ListenForEvents
        {
            get
            {
                return listenForEvents;
            }
            set
            {
                if (value == true)
                {
                    if (listenForEvents == false)
                    {
                        StartListening();
                    }
                }
                else
                {
                    StopListening();
                }

                listenForEvents = value;
            }
        }

        private UInt32 timeout;
        /// <summary>
        /// Timeout for publishing events
        /// </summary>
        [CLSCompliant(false)]
        public UInt32 Timeout
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

        private Callback callbackMethod;
        /// <summary>
        /// Callback delegate
        /// </summary>
        public Callback CallbackMethod
        {
            get
            {
                return callbackMethod;
            }
        }

        /// <summary>
        /// Size in bytes of the SharedQueue
        /// </summary>
        [CLSCompliant(false)]
        public UInt32 QueueSize
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
        /// <param name="subscriptionCallback">Handle to the SubscriptionCallback method</param>
        public SubscriptionManager(object subscriptionCallback)
            : this(defaultEventTimeout, subscriptionCallback)
        {
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="timeout">Timeout for publishing an event</param>
        /// <param name="subscriptionCallback">Handle to the SubscriptionCallback method</param>
        [CLSCompliant(false)]
        public SubscriptionManager(UInt32 timeout, object subscriptionCallback)
        {
            this.timeout = timeout;
            this.callbackMethod = new Callback((Callback)subscriptionCallback);

            try
            {
                lock (lockObject)
                {
                    publishMgrRefCount++;

                    if (publishMgr == null)
                    {
                        publishMgr = new PublishManager(timeout);
                    }
                }

                subscriptions = new Dictionary<Guid, Subscription>();

                eventQueue = new SharedQueue(eventQueueName, 100);

                if (eventQueue == null)
                {
                    ResourceManager rm = new ResourceManager("PubSubMgr.PubSubMgr", Assembly.GetExecutingAssembly());

                    throw new PubSubConnectionFailedException(rm.GetString("ConnectionFailed"));
                }

                listenForEvents = true;
                StartListening();
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
        /// Dispose for SubscriptionManager
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
        }

        /// <summary>
        /// Dispose for SubscriptionManager
        /// </summary>
        /// <param name="disposing">True if disposing.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                try
                {
                    this.StopListening();

                    Guid[] keys = GetSubscriptions();

                    foreach (Guid subId in keys)
                    {
                        RemoveSubscription(subId);
                    }

                    eventQueue.Dispose();
                    eventQueue = null;


                    lock (lockObject)
                    {
                        publishMgrRefCount--;

                        if (publishMgrRefCount == 0)
                        {
                            publishMgr.Dispose();
                            publishMgr = null;
                        }
                    }
                }

                catch
                {
                    // intentionally left empty
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
        ~SubscriptionManager()
        {
            Dispose(false);
        }

        /// <summary>
        /// Add a subscription for a specific event
        /// </summary>
        /// <param name="eventType">EventType being subscribed to</param>
        /// <param name="localOnly">Specifies if subscription is only for local machine or global</param>
        public void AddSubscription(Guid eventType, bool localOnly)
        {
            Subscription subscription = new Subscription();
            subscription.SubscriptionEventType = eventType;
            subscription.Subscribe = true;
            subscription.LocalOnly = localOnly;

            publishMgr.Publish(Subscription.SubscriptionEvent, subscription.Serialize());

            subscriptions[eventType] = subscription;
        }

        /// <summary>
        /// Remove a subscription for a specific event
        /// </summary>
        /// <param name="eventType">EventType being unsubscribed to</param>
        /// <returns>True if successful</returns>
        public bool RemoveSubscription(Guid eventType)
        {
            Subscription subscription = null;

            if (subscriptions.TryGetValue(eventType, out subscription) == true)
            {
                subscription.Subscribe = false;
                publishMgr.Publish(Subscription.SubscriptionEvent, subscription.Serialize());
                return subscriptions.Remove(eventType);
            }
            else
            {
                return true;
            }
        }

        /// <summary>
        /// Returns a list of EventTypes being subscribed to
        /// </summary>
        /// <returns>An array of EventTypes</returns>
        public Guid[] GetSubscriptions()
        {
            Guid[] keys = new Guid[subscriptions.Count];
            subscriptions.Keys.CopyTo(keys, 0);

            return keys;
        }

        /// <summary>
        /// Starts a thread to listen to events
        /// </summary>
        public void StartListening()
        {
            listenThread = new Thread(new ThreadStart(Listen));

            listenThread.Start();
        }

        /// <summary>
        /// Stops the thread listening to events
        /// </summary>
        public void StopListening()
        {
            if (listenThread != null)
            {
                try
                {
                    listenThread.Abort();
                }
                catch
                {
                }

                listenThread = null;
            }
        }

        /// <summary>
        /// Listening thread
        /// </summary>
        private void Listen()
        {
            string localRouterName;
            bool elementRetrieved;
            DateTime nextPushSubscriptions = DateTime.UtcNow.AddMinutes(subscriptionRefreshIncrement);
            Subscription subscription;
            WspEvent wspEvent;

            WspEvent[] wspEvents = null;

            try
            {
                Thread.CurrentThread.Priority = ThreadPriority.Highest;
                WaitCallback subscriptionCallback = new WaitCallback(CallSubscriptionCallback);

                byte[] buffer = null;

                localRouterName = Dns.GetHostName().ToLower();

                try
                {
                    char[] splitChar = { '.' };

                    IPHostEntry hostEntry = Dns.GetHostEntry(localRouterName);

                    string[] temp = hostEntry.HostName.ToLower().Split(splitChar, 2);

                    localRouterName = temp[0];
                }
                catch
                {
                }

                while (true)
                {
                    buffer = eventQueue.Dequeue(Timeout);

                    if (buffer == null)
                    {
                        elementRetrieved = false;
                    }
                    else
                    {
                        elementRetrieved = true;
                    }

                    if (elementRetrieved == true)
                    {
                        try
                        {
                            wspEvent = new WspEvent(buffer);
                        }
                        catch (Exception e)
                        {
                            EventLog.WriteEntry("WspEventRouter", "Event has invalid format:  " + e.ToString(), EventLogEntryType.Error);

                            continue;
                        }

                        if (subscriptions.TryGetValue(wspEvent.EventType, out subscription) == true)
                        {
                            if (subscription.LocalOnly == false || string.Compare(localRouterName, wspEvent.OriginatingRouterName, true) == 0)
                            {
                                StateInfo stateInfo = new StateInfo();

                                stateInfo.callBack = callbackMethod;
                                stateInfo.eventType = wspEvent.EventType;
                                stateInfo.wspEvent = wspEvent;

                                if (Interceptor.subscribeInterceptor != null)
                                {
                                    try
                                    {
                                        wspEvents = null;

                                        if (Interceptor.subscribeInterceptor(wspEvent, out wspEvents) == true)
                                        {
                                            if (wspEvents == null || wspEvents.Length == 0)
                                            {
                                                ThreadPool.QueueUserWorkItem(subscriptionCallback, stateInfo);
                                            }
                                            else
                                            {
                                                for (int i = 0; i < wspEvents.Length; i++)
                                                {
                                                    stateInfo = new StateInfo();

                                                    stateInfo.callBack = callbackMethod;
                                                    stateInfo.eventType = wspEvents[i].EventType;
                                                    stateInfo.wspEvent = wspEvents[i];

                                                    ThreadPool.QueueUserWorkItem(subscriptionCallback, stateInfo);
                                                }
                                            }
                                        }
                                    }
                                    catch
                                    {
                                    }
                                }
                                else
                                {
                                    ThreadPool.QueueUserWorkItem(subscriptionCallback, stateInfo);
                                }
                            }
                        }
                    }

                    if (DateTime.UtcNow > nextPushSubscriptions)
                    {
                        foreach (Guid subId in subscriptions.Keys)
                        {
                            try
                            {
                                publishMgr.Publish(Subscription.SubscriptionEvent, subscriptions[subId].Serialize());
                            }
                            catch
                            {
                                // intentionally left blank
                                // it will retry next time
                            }
                        }

                        nextPushSubscriptions = DateTime.UtcNow.AddMinutes(subscriptionRefreshIncrement);
                    }
                }
            }
            catch (ThreadAbortException)
            {
                // Another thread has signalled that this worker
                // thread must terminate.  Typically, this occurs when
                // the main service thread receives a service stop 
                // command.
            }
            catch (AccessViolationException)
            {
                // This can occur after the thread has been stopped and the runtime is doing GC.
                // Just let the thread quit to end listening.
            }
            catch (SharedQueueException e)
            {
                throw new PubSubException(e.Message, e.InnerException);
            }
        }

        /// <summary>
        /// Call the SubscriptionCallback with event
        /// </summary>
        private static void CallSubscriptionCallback(object stateInfo)
        {
            ((Callback)((StateInfo)stateInfo).callBack)(
                ((StateInfo)stateInfo).eventType, ((StateInfo)stateInfo).wspEvent);
        }
    }
}