using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Diagnostics;
using System.Resources;
using System.ServiceProcess;
using System.Threading;
using System.Text;
using System.Reflection;
using System.Reflection.Emit;
using System.Xml.XPath;

using Microsoft.WebSolutionsPlatform.Common;

[assembly: CLSCompliant(true)]

namespace Microsoft.WebSolutionsPlatform.Event
{
    /// <summary>
    /// Enum for different types of worker threads
    /// </summary>
    public enum WorkerThreadType : int
	{
        /// <summary>
        /// Not defined
        /// </summary>
        None = 0,
        /// <summary>
        /// This is the thread listening for new events to the queue
        /// </summary>
        ListenerThread = 1,
        /// <summary>
        /// This is the thread that communicates with the parent and children machines in the mesh
        /// </summary>
        CommunicatorThread = 2,
        /// <summary>
        /// This is the thread that takes incoming events from a parent/child machine and 
        /// publishes them to this machine.
        /// </summary>
		RePublisherThread = 3,
        /// <summary>
        /// This is the thread that manages the subscription routing table
        /// </summary>
		SubscriptionMgrThread = 4,
        /// <summary>
        /// This is the thread that persists events to the file system
        /// </summary>
		PersisterThread = 5,
        /// <summary>
        /// This is the thread that monitors the health of the other threads and restarts threads as 
        /// needed
        /// </summary>
		ManagerThread = 6
	}

    /// <summary>
    /// Struct containing two dictionary entries
    /// </summary>
    public struct DoubleDictionary<TDictionary1, TDictionary2>
	{
        private Dictionary<TDictionary1, DateTime> dictionary1;
        /// <summary>
        /// First dictionary of structure.
        /// </summary>
        public Dictionary<TDictionary1, DateTime> Dictionary1
        {
            get
            {
                return dictionary1;
            }

            set
            {
                dictionary1 = value;
            }
        }

        private Dictionary<TDictionary2, DateTime> dictionary2;
        /// <summary>
        /// First dictionary of structure.
        /// </summary>
        public Dictionary<TDictionary2, DateTime> Dictionary2
        {
            get
            {
                return dictionary2;
            }

            set
            {
                dictionary2 = value;
            }
        }
	}

	internal struct QueueElement
	{
		internal Guid EventType;
		internal Event Event;
		internal byte[] SerializedEvent;
        internal int SerializedLength;
		internal string OriginatingRouterName;
        internal string InRouterName;
    }

    /// <summary>
    /// Abstract class for worker threads
    /// </summary>
    public abstract class ServiceThread
	{
        /// <summary>
        /// The Start method is used to start the thread.
        /// </summary>
		public abstract void Start();
	}

    /// <summary>
    /// Main class for the Event Router
    /// </summary>
    public partial class Router : ServiceBase
	{
        internal static UInt32 eventQueueSize;
        internal static Int32 averageEventSize;

        internal static string eventQueueName;

        internal static SharedQueue eventQueue;

        internal static uint subscriptionRefreshIncrement = 3; // in minutes
        internal static uint subscriptionExpirationIncrement = 10; // in minutes

        internal static string thisNic;
        internal static int thisPort;
		internal static int thisBufferSize;
		internal static int thisTimeout; //Timeout in milliseconds (1000 = 1 second)

        internal static int thisOutQueueMaxSize = 102400000; // in bytes
        internal static int thisOutQueueMaxTimeout = 600; // in seconds

        private static string localRouterName = string.Empty;
		internal static string LocalRouterName
		{
			get
			{
                if (localRouterName.Length == 0)
                {
                    localRouterName = Dns.GetHostName();
                }

                return localRouterName;
			}
		}

        private static byte[] routerNameEncodedPriv;
        internal static byte[] routerNameEncoded
        {
            get
            {
                if (routerNameEncodedPriv == null)
                {
                    UnicodeEncoding uniEncoding = new UnicodeEncoding();
                    routerNameEncodedPriv = uniEncoding.GetBytes(Dns.GetHostName());
                }

                return routerNameEncodedPriv;
            }
        }

		internal static Route parentRoute;

		internal static Dictionary<Type, Thread> workerThreads;

        internal static Listener listener;
		internal static RePublisher rePublisher;
		internal static SubscriptionMgr subscriptionMgr;
		internal static Persister persister;
        internal static Communicator communicator;
		internal static Manager manager;

		internal static SynchronizationQueue<QueueElement> subscriptionMgrQueue;
		internal static SynchronizationQueue<QueueElement> rePublisherQueue;
		internal static SynchronizationQueue<QueueElement> persisterQueue;
		internal static SynchronizationQueue<QueueElement> forwarderQueue;

        internal static Dictionary<Guid, DoubleDictionary<string, Guid>> eventDictionary; //Route, Subscription
        internal static Dictionary<Guid, DoubleDictionary<Guid, string>> subscriptionDictionary; //EventType, Route
        internal static Dictionary<string, DoubleDictionary<Guid, Guid>> routerDictionary; //EventType, Subscription

        internal static Dictionary<string, string> channelDictionary; //RouterName, OutRouterName (channel)

        internal static string categoryName;
        internal static string communicationCategoryName;
        internal static string categoryHelp;
        internal static string communicationCategoryHelp;
        internal static string subscriptionQueueSizeName;
        internal static string rePublisherQueueSizeName;
        internal static string persisterQueueSizeName;
        internal static string forwarderQueueSizeName;
        internal static string subscriptionEntriesName;
        internal static string eventsProcessedName;
        internal static string eventsProcessedBytesName;
        internal static string baseInstance;

        internal static PerformanceCounter subscriptionQueueSize;
        internal static PerformanceCounter rePublisherQueueSize;
        internal static PerformanceCounter persisterQueueSize;
        internal static PerformanceCounter forwarderQueueSize;
        internal static PerformanceCounter subscriptionEntries;
        internal static PerformanceCounter eventsProcessed;
        internal static PerformanceCounter eventsProcessedBytes;

        /// <summary>
        /// Default constructor for the Router class
        /// </summary>
        public Router()
		{
            CounterCreationDataCollection CCDC;

            ResourceManager rm = new ResourceManager("Router.WspEventRouter", Assembly.GetExecutingAssembly());

            categoryName = rm.GetString("CategoryName");
            communicationCategoryName = rm.GetString("CommunicationCategoryName");
            categoryHelp = rm.GetString("CategoryHelp");
            communicationCategoryHelp = rm.GetString("CommunicationCategoryHelp");
            subscriptionQueueSizeName = rm.GetString("SubscriptionQueueSizeName");
            rePublisherQueueSizeName = rm.GetString("RePublisherQueueSizeName");
            persisterQueueSizeName = rm.GetString("PersisterQueueSizeName");
            forwarderQueueSizeName = rm.GetString("ForwarderQueueSizeName");
            subscriptionEntriesName = rm.GetString("SubscriptionEntriesName");
            eventsProcessedName = rm.GetString("EventsProcessedName");
            eventsProcessedBytesName = rm.GetString("EventsProcessedBytesName");
            baseInstance = rm.GetString("BaseInstance");

            subscriptionQueueSize = new PerformanceCounter(categoryName, subscriptionQueueSizeName, string.Empty, false);
            rePublisherQueueSize = new PerformanceCounter(categoryName, rePublisherQueueSizeName, string.Empty, false);
            persisterQueueSize = new PerformanceCounter(categoryName, persisterQueueSizeName, string.Empty, false);
            subscriptionEntries = new PerformanceCounter(categoryName, subscriptionEntriesName, string.Empty, false);
            eventsProcessed = new PerformanceCounter(categoryName, eventsProcessedName, string.Empty, false);
            eventsProcessedBytes = new PerformanceCounter(categoryName, eventsProcessedBytesName, string.Empty, false);

            if (PerformanceCounterCategory.Exists(communicationCategoryName) == false)
            {
                CCDC = new CounterCreationDataCollection();

                CounterCreationData forwarderQueueCounter = new CounterCreationData();
                forwarderQueueCounter.CounterType = PerformanceCounterType.NumberOfItems32;
                forwarderQueueCounter.CounterName = forwarderQueueSizeName;
                CCDC.Add(forwarderQueueCounter);

                PerformanceCounterCategory.Create(communicationCategoryName, communicationCategoryHelp,
                    PerformanceCounterCategoryType.MultiInstance, CCDC);
            }

            forwarderQueueSize = new PerformanceCounter(communicationCategoryName, forwarderQueueSizeName, baseInstance, false);

            subscriptionQueueSize.RawValue = 0;
            rePublisherQueueSize.RawValue = 0;
            persisterQueueSize.RawValue = 0;
            forwarderQueueSize.RawValue = 0;
            subscriptionEntries.RawValue = 0;
            eventsProcessed.RawValue = 0;
            eventsProcessedBytes.RawValue = 0;
            eventQueueSize = 10240;
            averageEventSize = 10240;

            eventQueueName = @"WspEventQueue";
            
			thisBufferSize = 1024000;
			thisTimeout = 10000;

            listener = new Listener();
			rePublisher = new RePublisher();
			subscriptionMgr = new SubscriptionMgr();
			persister = new Persister();
            communicator = new Communicator();
			manager = new Manager();

            subscriptionMgrQueue = new SynchronizationQueue<QueueElement>(2000, subscriptionQueueSize);
            rePublisherQueue = new SynchronizationQueue<QueueElement>(2000, rePublisherQueueSize);
            persisterQueue = new SynchronizationQueue<QueueElement>(2000, persisterQueueSize);
            forwarderQueue = new SynchronizationQueue<QueueElement>(2000, forwarderQueueSize);

			workerThreads = new Dictionary<Type, Thread>();

            workerThreads.Add(listener.GetType(), null);
            workerThreads.Add(rePublisher.GetType(), null);
            workerThreads.Add(subscriptionMgr.GetType(), null);
            workerThreads.Add(persister.GetType(), null);
            workerThreads.Add(communicator.GetType(), null);
            workerThreads.Add(manager.GetType(), null);

            eventDictionary = new Dictionary<Guid, DoubleDictionary<string, Guid>>();
            subscriptionDictionary = new Dictionary<Guid, DoubleDictionary<Guid, string>>();
            routerDictionary = new Dictionary<string, DoubleDictionary<Guid, Guid>>(StringComparer.CurrentCultureIgnoreCase);

            channelDictionary = new Dictionary<string, string>(StringComparer.CurrentCultureIgnoreCase);

			LoadConfiguration();

            this.ServiceName = "WspEventRouter";
        }

        /// <summary>
        /// Implements the OnStart for the service
        /// </summary>
        /// <param name="args"></param>
        protected override void OnStart(string[] args)
		{
			Thread workerThread = new Thread(new ThreadStart(manager.Start));

            workerThreads[manager.GetType()] = workerThread;

			workerThread.Start();
		}

        internal void Start()
        {
            Thread workerThread = new Thread(new ThreadStart(manager.Start));

            workerThreads[manager.GetType()] = workerThread;

            workerThread.Start();
        }

        /// <summary>
        /// Implements the OnStop for the service
        /// </summary>
		protected override void OnStop()
		{
            Thread[] threads = new Thread[workerThreads.Count];
            Thread managerThread = workerThreads[manager.GetType()];

            workerThreads.Values.CopyTo(threads, 0);

            if ((managerThread != null) && (managerThread.IsAlive))
            {
                managerThread.Abort();
            }

            foreach (Thread thread in threads)
            {
                if (thread != null && thread.IsAlive == true)
                {
                    thread.Abort();
                }
            }
        }

		/// <summary>
		/// Adds a Route to the routing table
		/// </summary>
        /// <param name="routerName">Name of the parent router</param>
		/// <param name="port">TCP port used by the router</param>
        /// <param name="bufferSize">Size of buffer for talking to router</param>
        /// <param name="timeout">Timeout for making TCP calls</param>
        internal static void AddRoute(string routerName, int port, int bufferSize, int timeout)
		{
			if(routerDictionary.ContainsKey(routerName) == false)
			{
                DoubleDictionary<Guid, Guid> values = new DoubleDictionary<Guid, Guid>();
                values.Dictionary1 = new Dictionary<Guid, DateTime>();
				values.Dictionary2 = new Dictionary<Guid, DateTime>();

				routerDictionary.Add(routerName, values);

                parentRoute = new Route(routerName, port, bufferSize, timeout);
			}
		}

		/// <summary>
		/// Adds a Route to the routing table
		/// </summary>
		/// <param name="routerName">routerName where subscription is made</param>
		/// <param name="eventType">eventType of the event</param>
		/// <param name="subscriptionId">subscriptionId of the event</param>
        internal static void AddRoute(string routerName, Guid eventType, Guid subscriptionId)
		{
            if (routerDictionary.ContainsKey(routerName) == false)
            {
                DoubleDictionary<Guid, Guid> values = new DoubleDictionary<Guid, Guid>();
                values.Dictionary1 = new Dictionary<Guid, DateTime>();
                values.Dictionary2 = new Dictionary<Guid, DateTime>();

                routerDictionary.Add(routerName, values);
            }

            routerDictionary[routerName].Dictionary1[eventType] = DateTime.UtcNow;
			routerDictionary[routerName].Dictionary2[subscriptionId] = DateTime.UtcNow;
		}

		/// <summary>
		/// Deletes a Route from the routing table
		/// </summary>
		/// <param name="routerName">routerName of the router</param>
		internal static void DeleteRoute( string routerName )
		{
            foreach (Guid eventType in routerDictionary[routerName].Dictionary1.Keys)
			{
				eventDictionary[eventType].Dictionary1.Remove(routerName);
			}

			foreach(Guid subscriptionId in routerDictionary[routerName].Dictionary2.Keys)
			{
				subscriptionDictionary[subscriptionId].Dictionary2.Remove(routerName);
			}

			routerDictionary.Remove(routerName);

            if (parentRoute.RouterName == routerName)
            {
                parentRoute = null;
            }
		}

		/// <summary>
		/// Adds an EventType to the routing table
		/// </summary>
		/// <param name="eventType">eventType of the event</param>
		/// <param name="routerName">routerName where subscription is made</param>
		/// <param name="subscriptionId">subscriptionId of the event</param>
        internal static void AddEvent(Guid eventType, string routerName, Guid subscriptionId)
		{
			if(eventDictionary.ContainsKey(eventType) == false)
			{
                DoubleDictionary<string, Guid> values = new DoubleDictionary<string, Guid>();
                values.Dictionary1 = new Dictionary<string, DateTime>(StringComparer.CurrentCultureIgnoreCase);
                values.Dictionary2 = new Dictionary<Guid, DateTime>();

				eventDictionary.Add(eventType, values);
			}

			eventDictionary[eventType].Dictionary1[routerName] = DateTime.UtcNow;
			eventDictionary[eventType].Dictionary2[subscriptionId] = DateTime.UtcNow;
		}

		/// <summary>
		/// Deletes an EventType from the routing table
		/// </summary>
		/// <param name="eventType">eventType of the event</param>
        internal static void DeleteEvent(Guid eventType)
		{
            foreach (string routerName in eventDictionary[eventType].Dictionary1.Keys)
            {
                routerDictionary[routerName].Dictionary1.Remove(eventType);

                foreach (Guid subscriptionId in routerDictionary[routerName].Dictionary2.Keys)
                {
                    subscriptionDictionary[subscriptionId].Dictionary1.Remove(eventType);
                }
            }

            eventDictionary.Remove(eventType);
        }

		/// <summary>
		/// Adds a Subscription to the routing table
		/// </summary>
		/// <param name="subscriptionId">subscriptionId of the event</param>
		/// <param name="eventType">eventType of the subscription</param>
		/// <param name="routerName">routerName where subscription is made</param>
		/// <param name="localOnly">Defines if subscription is for local machine only</param>
        internal static void AddSubscription(Guid subscriptionId, Guid eventType, string routerName, bool localOnly)
		{
			if(subscriptionDictionary.ContainsKey(subscriptionId) == false)
			{
                DoubleDictionary<Guid, string> values = new DoubleDictionary<Guid, string>();
                values.Dictionary1 = new Dictionary<Guid, DateTime>();
                values.Dictionary2 = new Dictionary<string, DateTime>(StringComparer.CurrentCultureIgnoreCase);

				subscriptionDictionary.Add(subscriptionId, values);

				SubscriptionMgr.subscriptions.Add(subscriptionId, new SubscriptionEntry(subscriptionId, eventType, routerName, localOnly));

                subscriptionEntries.Increment();
			}

			subscriptionDictionary[subscriptionId].Dictionary1[eventType] = DateTime.UtcNow;
			AddEvent(eventType, routerName, subscriptionId);

			subscriptionDictionary[subscriptionId].Dictionary2[routerName] = DateTime.UtcNow;
			AddRoute(routerName, eventType, subscriptionId);
		}

		/// <summary>
		/// Deletes a Subscription from the routing table
		/// </summary>
		/// <param name="subscriptionId">subscriptionId of the event</param>
		internal static void DeleteSubscription( Guid subscriptionId )
		{
            foreach (Guid eventType in subscriptionDictionary[subscriptionId].Dictionary1.Keys)
			{
				eventDictionary[eventType].Dictionary2.Remove(subscriptionId);
			}

			foreach(string routerName in subscriptionDictionary[subscriptionId].Dictionary2.Keys)
			{
				routerDictionary[routerName].Dictionary2.Remove(subscriptionId);
			}

			subscriptionDictionary.Remove(subscriptionId);

			SubscriptionMgr.subscriptions.Remove(subscriptionId);

            subscriptionEntries.Decrement();
		}

		/// <summary>
		/// Concatenates an ArrayList of byte[] and returns one byte[]
		/// </summary>
		/// <param name="arrayIn">ArrayList of byte[]</param>
		public static byte[] ConcatArrayList( ArrayList arrayIn )
		{
			int size = 0;
			int location = 0;

            if (arrayIn.Count == 1)
            {
                return (byte[])arrayIn[0];
            }

			for(int i = 0; i < arrayIn.Count; i++)
			{
				size = size + ((byte[])arrayIn[i]).Length;
			}

			byte[] arrayOut = new byte[size];

			for(int i = 0; i < arrayIn.Count; i++)
			{
				((byte[])arrayIn[i]).CopyTo(arrayOut, location);

				location = location + ((byte[])arrayIn[i]).Length;
			}

			return arrayOut;
		}
	}

	internal class Route : IComparable<Route>
	{
		private string routerName;
		/// <summary>
		/// Name of the router
		/// </summary>
        public string RouterName
		{
			get
			{
                return routerName;
			}
		}

		private int port;
		/// <summary>
		/// TCP port for route
		/// </summary>
		public int Port
		{
			get
			{
				return port;
			}
			internal set
			{
				port = value;
			}
		}

		private int bufferSize;
		/// <summary>
		/// Buffer size used for the TCP port for route
		/// </summary>
		public int BufferSize
		{
			get
			{
				return bufferSize;
			}
			internal set
			{
				bufferSize = value;
			}
		}

		private int timeout;
		/// <summary>
		/// Timeout used for TCP calls
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

		private DateTime expirationTime;
		/// <summary>
		/// Expiration time for route
		/// </summary>
		public DateTime ExpirationTime
		{
			get
			{
				return expirationTime;
			}
			internal set
			{
				expirationTime = value;
			}
		}

		/// <summary>
		/// Used to create a Route used by RouteMgr
		/// </summary>
        /// <param name="routerName">Name of the router</param>
		/// <param name="port">TCP port used by the router</param>
		/// <param name="bufferSize">Buffer size used for the TCP port for route</param>
		/// <param name="timeout">Timeout used for TCP calls</param>
        public Route(string routerName, int port, int bufferSize, int timeout)
		{
            this.routerName = routerName;
			this.port = port;
			this.bufferSize = bufferSize;
			this.timeout = timeout;
			this.expirationTime = DateTime.UtcNow.AddMinutes(5);
		}

		public int CompareTo( Route otherRoute )
		{
            return RouterName.CompareTo(otherRoute.RouterName);
		}
	}
}
