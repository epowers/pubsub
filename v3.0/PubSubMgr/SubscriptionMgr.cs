using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Net;
using System.Resources;
using System.Reflection;
using Microsoft.CSharp;
using System.CodeDom.Compiler;
using Microsoft.WebSolutionsPlatform.Common;

namespace Microsoft.WebSolutionsPlatform.PubSubManager
{
    /// <summary>
    /// Delegate for the method used for filtered subscriptions
    /// </summary>
    /// <param name="wspEvent"></param>
    /// <returns></returns>
    public delegate bool WspFilterMethod(WspEvent wspEvent);

    internal class SubscriptionObserver
    {
        internal Guid id;
        internal IObserver<WspEvent> observer;
        internal SynchronizationQueueGeneric<WspEvent> queue;
        internal PerformanceCounter eventQueueCounter;
        internal Thread observerThread;

        internal SubscriptionObserver(IObserver<WspEvent> observer)
        {
            this.id = Guid.NewGuid();
            this.observer = observer;

            eventQueueCounter = new PerformanceCounter();
            eventQueueCounter.InstanceLifetime = PerformanceCounterInstanceLifetime.Process;
            eventQueueCounter.CategoryName = "WspEventRouterApplication";
            eventQueueCounter.CounterName = "SubscriptionQueueSize";
            eventQueueCounter.InstanceName = "PID" + Process.GetCurrentProcess().Id.ToString() + ":" + this.id.ToString();
            eventQueueCounter.ReadOnly = false;

            this.queue = new SynchronizationQueueGeneric<WspEvent>(eventQueueCounter, Guid.Empty);

            this.observerThread = new Thread(Start);
            observerThread.Start();
        }

        internal void Start()
        {
            WspEvent wspEvent;

            try
            {
                while (true)
                {
                    try
                    {
                        wspEvent = this.queue.Dequeue();

                        if (wspEvent == null)
                        {
                            continue;
                        }

                        try
                        {
                            this.observer.OnNext(wspEvent);
                        }
                        catch (Exception e)
                        {
                            EventLog.WriteEntry("WspEventRouter", "Application threw unhandled exception:  " + e.ToString(), EventLogEntryType.Warning);
                        }
                    }
                    catch (InvalidOperationException)
                    {
                        wspEvent = null;
                    }
                    catch (Exception e)
                    {
                        EventLog.WriteEntry("WspEventRouter", "Exception processing event:  " + e.ToString(), EventLogEntryType.Warning);

                        continue;
                    }
                }
            }
            catch
            {
                // intentionally left blank
            }
        }
    }

    /// <summary>
    /// This class is used to subscribe to events using Rx
    /// </summary>
    public class WspEventObservable : IObservable<WspEvent>, IDisposable
    {
        private static Thread wspSubscriptionThread = null;

        private static object lockObj = new object();

        private static Guid initialEventType;
        private static bool initialLocalOnly;
        private static string initialMethodBody;
        private static List<string> initialUsingLibraries;
        private static List<string> initialReferencedAssemblies;

        private static SubscriptionManager wspSubscriptionManager = null;

        private static Dictionary<Guid, List<SubscriptionObserver>> observables = new Dictionary<Guid, List<SubscriptionObserver>>();

        internal static Dictionary<Guid, List<WspEventObservable>> eventObservables = new Dictionary<Guid, List<WspEventObservable>>();

        private Guid id;
        private Guid eventType;
        private bool localOnly;
        private string methodBody;
        private List<string> usingLibraries;
        private List<string> referencedAssemblies;
        private WspFilterMethod filterMethod;
        internal SynchronizationQueueGeneric<WspEvent> queue;
        internal PerformanceCounter eventQueueCounter = null;
        internal Thread observableThread = null;

        /// <summary>
        /// The ObservableSubscription constructor is used to create an observable subscription to then subscribe to via Rx
        /// </summary>
        /// <param name="eventType">The event type to subscribe to</param>
        /// <param name="localOnly">True if only local events are to be subscribed to</param>
        public WspEventObservable(Guid eventType, bool localOnly)
            : this(eventType, localOnly, null, null, null)
        {
        }

        /// <summary>
        /// The ObservableSubscription constructor is used to create an observable filtered subscription to then subscribe to via Rx
        /// </summary>
        /// <remarks>
        /// The method will obviously consume more overhead, so only use when necessary. If referenced assemblies, they must be
        /// deployed and accessible by the WspEventRouter on ALL computers running Wsp.
        /// </remarks>
        /// <param name="eventType">The event type to subscribe to</param>
        /// <param name="localOnly">True if only local events are to be subscribed to</param>
        /// <param name="methodBody">Method body defining the filter</param>
        /// <param name="usingLibraries">List of using libraries which the method requires, null if none required</param>
        /// <param name="referencedAssemblies">List of referenced assemblies which the method requires, null if none required</param>
        public WspEventObservable(Guid eventType, bool localOnly, string methodBody, List<string> usingLibraries, List<string> referencedAssemblies)
        {
            this.id = Guid.NewGuid();
            this.eventType = eventType;
            this.localOnly = localOnly;
            this.usingLibraries = null;
            this.referencedAssemblies = null;
            this.filterMethod = null;

            if (string.IsNullOrEmpty(methodBody) == true)
            {
                this.methodBody = string.Empty;
            }
            else
            {
                this.methodBody = methodBody;

                if (usingLibraries != null)
                {
                    this.usingLibraries = new List<string>(usingLibraries.Count);
                    for (int i = 0; i < usingLibraries.Count; i++)
                    {
                        this.usingLibraries[i] = usingLibraries[i];
                    }
                }

                if (referencedAssemblies != null)
                {
                    this.referencedAssemblies = new List<string>(referencedAssemblies.Count);
                    for (int i = 0; i < referencedAssemblies.Count; i++)
                    {
                        this.referencedAssemblies[i] = referencedAssemblies[i];
                    }
                }

                CompilerResults results;

                bool rc = CompileFilterMethod(this.methodBody, this.usingLibraries, this.referencedAssemblies, out this.filterMethod, out results);

                if (rc == false)
                {
                    throw new PubSubCompileException(results.Errors.ToString());
                }
            }

            eventQueueCounter = new PerformanceCounter();
            eventQueueCounter.InstanceLifetime = PerformanceCounterInstanceLifetime.Process;
            eventQueueCounter.CategoryName = "WspEventRouterApplication";
            eventQueueCounter.CounterName = "SubscriptionQueueSize";
            eventQueueCounter.InstanceName = "PID" + Process.GetCurrentProcess().Id.ToString() + ":" + eventType.ToString() + ":" + Guid.NewGuid().ToString().GetHashCode().ToString();
            eventQueueCounter.ReadOnly = false;

            this.queue = new SynchronizationQueueGeneric<WspEvent>(eventQueueCounter, eventType);

            observableThread = new Thread(ObservableThread);
            observableThread.Start();

            lock (lockObj)
            {
                if (wspSubscriptionThread == null)
                {
                    wspSubscriptionManager = new SubscriptionManager();
                    wspSubscriptionThread = new Thread(wspSubscriptionManager.Listener);
                    wspSubscriptionThread.Start();
                }
            }
        }

        /// <summary>
        /// The Dispose needs to be called in order to remove the associated perf counter resources
        /// </summary>
        public void Dispose()
        {
            try
            {
                if (this.eventQueueCounter != null)
                {
                    this.eventQueueCounter.RemoveInstance();
                    this.eventQueueCounter = null;
                }
            }
            catch
            {
                // Intentionally left blank
            }
        }

        ~WspEventObservable()
        {
            Dispose();
        }

        private void ObservableThread()
        {
            WspEvent wspEvent;
            WspEvent[] wspEvents = null;

            try
            {
                while (true)
                {
                    try
                    {
                        wspEvent = this.queue.Dequeue();

                        if (wspEvent == null)
                        {
                            continue;
                        }

                        if (this.filterMethod != null)
                        {
                            if (this.filterMethod(wspEvent) == false)
                            {
                                continue;
                            }
                        }

                        if (Interceptor.subscribeInterceptor != null)
                        {
                            wspEvents = null;

                            try
                            {
                                if (Interceptor.subscribeInterceptor(wspEvent, this, out wspEvents) == false)
                                {
                                    continue;
                                }
                            }
                            catch (Exception e)
                            {
                                EventLog.WriteEntry("WspEventRouter", "Interceptor threw unhandled exception:  " + e.ToString(), EventLogEntryType.Warning);
                            }
                        }

                        List<SubscriptionObserver> observers;

                        if (wspEvents == null)
                        {
                            lock (lockObj)
                            {
                                if (observables.TryGetValue(this.id, out observers) == true)
                                {
                                    for (int i = 0; i < observers.Count; i++)
                                    {
                                        observers[i].queue.Enqueue(wspEvent, wspEvent.SerializedEvent.Length);
                                    }
                                }
                            }
                        }
                        else
                        {
                            lock (lockObj)
                            {
                                for (int x = 0; x < wspEvents.Length; x++)
                                {
                                    if (observables.TryGetValue(this.id, out observers) == true)
                                    {
                                        for (int i = 0; i < observers.Count; i++)
                                        {
                                            observers[i].queue.Enqueue(wspEvents[x], wspEvents[x].SerializedEvent.Length);
                                        }
                                    }
                                }
                            }
                        }
                    }
                    catch (InvalidOperationException)
                    {
                        wspEvent = null;
                    }
                    catch (Exception e)
                    {
                        EventLog.WriteEntry("WspEventRouter", "Exception processing event:  " + e.ToString(), EventLogEntryType.Warning);

                        continue;
                    }
                }
            }
            catch
            {
                // intentionally left blank
            }
        }

        /// <summary>
        /// Subscribe method to provide an observer
        /// </summary>
        /// <param name="observer">The observer to be called for each event</param>
        /// <returns></returns>
        public IDisposable Subscribe(IObserver<WspEvent> observer)
        {
            SubscriptionObserver subscriptionObserver;

            lock (lockObj)
            {
                if (eventObservables.Count == 0)
                {
                    initialEventType = eventType;
                    initialLocalOnly = localOnly;
                    initialMethodBody = methodBody;
                    initialUsingLibraries = usingLibraries;
                    initialReferencedAssemblies = referencedAssemblies;
                }

                wspSubscriptionManager.AddSubscription(eventType, localOnly, methodBody, usingLibraries, referencedAssemblies);

                subscriptionObserver = new SubscriptionObserver(observer);

                List<SubscriptionObserver> observers;

                if (observables.TryGetValue(this.id, out observers) == false)
                {
                    observers = new List<SubscriptionObserver>();
                    observables[this.id] = observers;
                }

                observers.Add(subscriptionObserver);

                List<WspEventObservable> observableList;

                if (eventObservables.TryGetValue(this.eventType, out observableList) == false)
                {
                    observableList = new List<WspEventObservable>();
                    eventObservables[this.eventType] = observableList;
                }

                bool observableExists = false;

                for (int i = 0; i < observableList.Count; i++)
                {
                    if (observableList[i].id == this.id)
                    {
                        observableExists = true;
                        break;
                    }
                }

                if (observableExists == false)
                {
                    observableList.Add(this);
                }
            }

            return new WspSubscriptionDisposable(this, subscriptionObserver);
        }

        /// <summary>
        /// Compiles the method for the subscription and returns the function.
        /// </summary>
        /// <param name="methodBodyString">String containing the method body to compile</param>
        /// <param name="usingLibraries">List of the using libraries by the expression</param>
        /// <param name="referencedAssemblies">List of the reference assemblies used by the expression</param>
        /// <param name="filterMethod">The compiled function</param>
        /// <param name="results">Compiler results</param>
        /// <returns>true if successful, false if failed</returns>
        public static bool CompileFilterMethod(string methodBodyString, List<string> usingLibraries, List<string> referencedAssemblies,
            out WspFilterMethod filterMethod, out CompilerResults results)
        {
            Dictionary<string, string> usingDictionary = new Dictionary<string, string>(StringComparer.CurrentCultureIgnoreCase);
            Dictionary<string, string> referencedDictionary = new Dictionary<string, string>(StringComparer.CurrentCultureIgnoreCase);

            filterMethod = null;
            results = null;

            string className = "a" + Guid.NewGuid().ToString().Replace('-', 'a');

            try
            {
                usingDictionary[@"using System;"] = null;
                usingDictionary[@"using System.Linq;"] = null;
                usingDictionary[@"using System.Linq.Expressions;"] = null;
                usingDictionary[@"using Microsoft.WebSolutionsPlatform.Event;"] = null;
                usingDictionary[@"using Microsoft.WebSolutionsPlatform.PubSubManager;"] = null;

                if (usingLibraries != null)
                {
                    for (int i = 0; i < usingLibraries.Count; i++)
                    {
                        if (usingLibraries[i].EndsWith(";") == true)
                        {
                            usingDictionary[usingLibraries[i]] = null;
                        }
                        else
                        {
                            usingDictionary[usingLibraries[i] + ";"] = null;
                        }
                    }
                }

                referencedDictionary[@"System.dll"] = null;
                referencedDictionary[@"System.Core.dll"] = null;
                referencedDictionary[AppDomain.CurrentDomain.SetupInformation.ApplicationBase + @"WspEvent.dll"] = null;
                referencedDictionary[AppDomain.CurrentDomain.SetupInformation.ApplicationBase + @"WspPubSubMgr.dll"] = null;

                if (referencedAssemblies != null)
                {
                    for (int i = 0; i < referencedAssemblies.Count; i++)
                    {
                        referencedDictionary[referencedAssemblies[i]] = null;
                    }
                }

                string[] code = new string[1];

                StringBuilder sb = new StringBuilder();

                foreach (string s in usingDictionary.Keys)
                {
                    sb.Append(s + "\n");
                }

                sb.Append("public class " + className + "\n");
                sb.Append("{\n");
                sb.Append("    public WspFilterMethod GetFilterFunction()\n");
                sb.Append("    {\n");
                sb.Append("        return FilterMethod;\n");
                sb.Append("    }\n");
                sb.Append("\n");
                sb.Append("    public bool FilterMethod(WspEvent wspEvent)\n");
                sb.Append("    {\n");
                sb.Append(methodBodyString);
                sb.Append("    }\n");
                sb.Append("}\n");

                code[0] = sb.ToString();

                CSharpCodeProvider compiler = new CSharpCodeProvider();

                CompilerParameters parms = new System.CodeDom.Compiler.CompilerParameters
                {
                    GenerateExecutable = false,
                    GenerateInMemory = true
                };

                foreach (string asm in referencedDictionary.Keys)
                {
                    parms.ReferencedAssemblies.Add(asm);
                }

                results = compiler.CompileAssemblyFromSource(parms, code);

                if (results.Errors.HasErrors == true)
                {
                    return false;
                }
                else
                {
                    var wspFilterClass = results.CompiledAssembly.CreateInstance(className);
                    Type filterType = wspFilterClass.GetType();
                    MethodInfo tempMethod = filterType.GetMethod("GetFilterFunction");
                    ConstructorInfo filterConstructor = filterType.GetConstructor(Type.EmptyTypes);
                    object filterObject = filterConstructor.Invoke(new object[] { });

                    filterMethod = (WspFilterMethod)tempMethod.Invoke(filterObject, new object[] { });

                    WspEvent wspEvent = new WspEvent(Guid.Empty, null, null);

                    filterMethod(wspEvent);

                    return true;
                }
            }
            catch
            {
                return false;
            }
        }

        class WspSubscriptionDisposable : IDisposable
        {
            private readonly WspEventObservable parent;
            private readonly SubscriptionObserver subscriptionObserver;

            public WspSubscriptionDisposable(WspEventObservable parent, SubscriptionObserver subscriptionObserver)
            {
                this.parent = parent;
                this.subscriptionObserver = subscriptionObserver;
            }

            public void Dispose()
            {
                parent.Dispose(subscriptionObserver);
            }
        }

        /// <summary>
        /// Dispose for the ObservableSubscription object
        /// </summary>
        /// <param name="subscriptionObserver">Observer to be disposed</param>
        private void Dispose(SubscriptionObserver subscriptionObserver)
        {
            lock (lockObj)
            {
                List<SubscriptionObserver> observers;

                if (observables.TryGetValue(this.id, out observers) == true)
                {
                    for (int i = 0; i < observers.Count; i++)
                    {
                        if (subscriptionObserver.id == observers[i].id)
                        {
                            observers.RemoveAt(i);

                            break;
                        }
                    }

                    if (observers.Count == 0)
                    {
                        observables.Remove(this.id);

                        List<WspEventObservable> observableList;

                        if (eventObservables.TryGetValue(this.eventType, out observableList) == true)
                        {
                            for (int i = 0; i < observableList.Count; i++)
                            {
                                if (observableList[i].id == this.id)
                                {
                                    observableList.RemoveAt(i);

                                    break;
                                }
                            }

                            if (observableList.Count == 0)
                            {
                                eventObservables.Remove(this.eventType);
                            }
                        }

                        this.Dispose();
                    }

                    wspSubscriptionManager.RemoveSubscription(eventType, localOnly, methodBody, usingLibraries, referencedAssemblies);

                    this.observableThread.Abort();
                }

                try
                {
                    subscriptionObserver.observerThread.Abort();
                    subscriptionObserver.eventQueueCounter.RemoveInstance();
                }
                catch
                {
                }

                if (observables.Count == 0 && wspSubscriptionThread != null)
                {
                    wspSubscriptionManager.StopListening = true;
                    wspSubscriptionThread.Join(30000);
                    wspSubscriptionThread = null;

                    wspSubscriptionManager = null;
                }
            }
        }
    }

    /// <summary>
    /// This class is used to subscribe to events.
    /// </summary>
    internal class SubscriptionManager
    {
        private bool disposed;

        internal bool StopListening = false;

        private static UInt32 defaultEventTimeout = 10000;
        private static uint subscriptionRefreshIncrement = 3; // in minutes

        private static PublishManager publishMgr;
        private static int publishMgrRefCount = 0;

        private static object lockObject = new object();

        private string eventQueueName = @"WspEventQueue";
        private SharedQueue eventQueue;

        private Dictionary<Guid, Dictionary<string, Subscription>> subscriptions;

        private UInt32 timeout;
        /// <summary>
        /// Timeout for publishing events
        /// </summary>
        //[CLSCompliant(false)]
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

        /// <summary>
        /// Size in bytes of the SharedQueue
        /// </summary>
        //[CLSCompliant(false)]
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
        internal SubscriptionManager()
            : this(defaultEventTimeout)
        {
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="timeout">Timeout for publishing an event</param>
        //[CLSCompliant(false)]
        internal SubscriptionManager(UInt32 timeout)
        {
            this.timeout = timeout;

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

                subscriptions = new Dictionary<Guid, Dictionary<string, Subscription>>();

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
                    lock (lockObject)
                    {
                        foreach (Dictionary<string, Subscription> subs in subscriptions.Values)
                        {
                            foreach (Subscription sub in subs.Values)
                            {
                                RemoveSubscription(sub.SubscriptionEventType, sub.LocalOnly, sub.MethodBody, sub.UsingLibraries, sub.ReferencedAssemblies);
                            }
                        }

                        eventQueue.Dispose();
                        eventQueue = null;

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
        /// <param name="methodBody">String containing the method body defining the filter</param>
        /// <param name="usingLibraries">List of using libraries which the method requires, null if none required</param>
        /// <param name="referencedAssemblies">List of referenced assemblies which the method requires, null if none required</param>
        public void AddSubscription(Guid eventType, bool localOnly, string methodBody, List<string> usingLibraries, List<string> referencedAssemblies)
        {
            ReturnCode rc;

            Subscription subscription = new Subscription();
            subscription.SubscriptionEventType = eventType;
            subscription.Subscribe = true;
            subscription.LocalOnly = localOnly;
            subscription.MethodBody = methodBody;
            subscription.UsingLibraries = usingLibraries;
            subscription.ReferencedAssemblies = referencedAssemblies;

            WspEvent wspEvent = new WspEvent(Subscription.SubscriptionEvent, null, subscription.Serialize());

            publishMgr.Publish(wspEvent.SerializedEvent, out rc);

            Dictionary<string, Subscription> subscriptionFilter;

            if (subscriptions.TryGetValue(eventType, out subscriptionFilter) == false)
            {
                subscriptions[eventType] = new Dictionary<string, Subscription>();
            }

            subscriptions[eventType][methodBody] = subscription;
        }

        /// <summary>
        /// Remove a subscription for a specific event
        /// </summary>
        /// <param name="eventType">EventType being unsubscribed to</param>
        /// <param name="localOnly">Specifies if subscription is only for local machine or global</param>
        /// <param name="methodBody">String containing the method body defining the filter</param>
        /// <param name="usingLibraries">List of using libraries which the method requires, null if none required</param>
        /// <param name="referencedAssemblies">List of referenced assemblies which the method requires, null if none required</param>
        /// <returns>True if successful</returns>
        public bool RemoveSubscription(Guid eventType, bool localOnly, string methodBody, List<string> usingLibraries, List<string> referencedAssemblies)
        {
            ReturnCode rc;

            Subscription subscription = null;

            Dictionary<string, Subscription> subscriptionFilter;

            if (subscriptions.TryGetValue(eventType, out subscriptionFilter) == true)
            {
                if (subscriptionFilter.TryGetValue(methodBody, out subscription) == true)
                {
                    subscription.Subscribe = false;

                    WspEvent wspEvent = new WspEvent(Subscription.SubscriptionEvent, null, subscription.Serialize());

                    publishMgr.Publish(wspEvent.SerializedEvent, out rc);

                    subscriptionFilter.Remove(methodBody);

                    if (subscriptionFilter.Count == 0)
                    {
                        subscriptions.Remove(eventType);
                    }
                }
            }

            return true;
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
        /// Dequeues events from shared memory
        /// </summary>
        internal void Listener()
        {
            WspEvent wspEvent;
            DateTime nextPushSubscriptions = DateTime.UtcNow.AddMinutes(subscriptionRefreshIncrement);
            ReturnCode rc;

            try
            {
                Thread.CurrentThread.Priority = ThreadPriority.Highest;

                byte[] buffer = null;

                while (StopListening == false)
                {
                    try
                    {
                        buffer = eventQueue.Dequeue(Timeout);

                        if (buffer != null)
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

                            List<WspEventObservable> observables;

                            if (WspEventObservable.eventObservables.TryGetValue(wspEvent.EventType, out observables) == true)
                            {
                                for (int i = 0; i < observables.Count; i++)
                                {
                                    observables[i].queue.Enqueue(wspEvent, wspEvent.SerializedEvent.Length);
                                }
                            }
                        }

                        if (DateTime.UtcNow > nextPushSubscriptions)
                        {
                            try
                            {
                                foreach (Guid subId in subscriptions.Keys)
                                {
                                    foreach (string expression in subscriptions[subId].Keys)
                                    {

                                        WspEvent subEvent = new WspEvent(Subscription.SubscriptionEvent, null, subscriptions[subId][expression].Serialize());

                                        publishMgr.Publish(subEvent.SerializedEvent, out rc);
                                    }
                                }

                                nextPushSubscriptions = DateTime.UtcNow.AddMinutes(subscriptionRefreshIncrement);
                            }
                            catch
                            {
                                // intentionally left blank
                                // it will retry next time
                            }
                        }
                    }
                    catch (PubSubException e)
                    {
                        EventLog.WriteEntry("WspEventRouter", "Exception occurred processing event from shared queue: " + e.ToString(), EventLogEntryType.Error);

                        continue;
                    }
                    catch (Exception e)
                    {
                        EventLog.WriteEntry("WspEventRouter", "Exception occurred processing event from shared queue: " + e.ToString(), EventLogEntryType.Error);

                        continue;
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
    }
}