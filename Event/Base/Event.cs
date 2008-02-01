using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Net;
using System.Net.Sockets;
using System.Reflection;

[assembly: CLSCompliant(true)]
namespace Microsoft.WebSolutionsPlatform.Event
{
    /// <summary>
    /// Structure which defines the WspKeyValuePair used by enumerating the data
    /// in a serialized object.
    /// </summary>
    /// <typeparam name="TypeKey"></typeparam>
    /// <typeparam name="TypeValue"></typeparam>
    public struct WspKeyValuePair<TypeKey, TypeValue>
    {
        private TypeKey key;
        /// <summary>
        /// Key of the struct
        /// </summary>
        public TypeKey Key
        {
            get
            {
                return key;
            }

            set
            {
                key = value;
            }
        }

        private TypeValue valueIn;
        /// <summary>
        /// ValueIn of the struct
        /// </summary>
        public TypeValue ValueIn
        {
            get
            {
                return valueIn;
            }

            set
            {
                valueIn = value;
            }
        }

        /// <summary>
        /// Base constructor to create a new WspKeyValuePair
        /// </summary>
        /// <param name="key">Key of the struct</param>
        /// <param name="valueIn">Initial value of the struct</param>
        public WspKeyValuePair(TypeKey key, TypeValue valueIn)
        {
            this.key = key;
            this.valueIn = valueIn;
        }
    }

    /// <summary>
    /// Base class for events. All events must inherit from this class.
    /// </summary>
    abstract public class Event : IDisposable
	{
        private bool disposed;

        private static string baseVersion = @"1.0.0.0";

        private string originatingRouterName = string.Empty;
        /// <summary>
        /// Router that the event originated from
        /// </summary>
        public string OriginatingRouterName
        {
            get
            {
                if (originatingRouterName.Length == 0)
                {
                    originatingRouterName = Dns.GetHostName();
                }

                return originatingRouterName;
            }

            set
            {
                originatingRouterName = value;
            }
        }

        private string inRouterName = string.Empty;
        /// <summary>
        /// Router the event was passed from
        /// </summary>
        public string InRouterName
        {
            get
            {
                if (inRouterName.Length == 0)
                {
                    inRouterName = Dns.GetHostName();
                }

                return inRouterName;
            }

            set
            {
                inRouterName = value;
            }
        }

        private static Guid subscriptionEvent = new Guid(@"3D7B4317-C051-4e1a-8379-B6E2D6C107F9");
        /// <summary>
        /// Event type for a Subscription Event
        /// </summary>
        public static Guid SubscriptionEvent
        {
            get
            {
                return subscriptionEvent;
            }

            set
            {
                subscriptionEvent = value;
            }
        }

        private Guid eventType;
		/// <summary>
		/// Type of the event
		/// </summary>
        public Guid EventType
		{
			get
			{
				return eventType;
			}

			set
			{
				eventType = value;
			}
		}

		private Version eventVersion;
		/// <summary>
		/// Version of the event
		/// </summary>
		public Version EventVersion
		{
			get
			{
				return eventVersion;
			}

			set
			{
				eventVersion = value;
			}
		}

		private string eventName;
		/// <summary>
		/// Friendly name of the event
		/// </summary>
		public string EventName
		{
			get
			{
				return eventName;
			}

			set
			{
				eventName = value;
			}
		}

		private long eventTime;
		/// <summary>
		/// UTC time in ticks of when the event is published
		/// </summary>
		public long EventTime
		{
			get
			{
				return eventTime;
			}

            set
            {
                eventTime = value;
            }
        }

		private string eventPublisher;
		/// <summary>
		/// Friendly name of the event
		/// </summary>
		public string EventPublisher
		{
			get
			{
				return eventPublisher;
			}

			set
			{
				eventPublisher = value;
			}
		}

		private SerializationData serializedEvent;
		/// <summary>
		/// Serialized version of the event
		/// </summary>
		public SerializationData SerializedEvent
		{
			get
			{
				return serializedEvent;
			}

            set
            {
                serializedEvent = value;
            }
        }

		/// <summary>
		/// Base constructor to create a new event
		/// </summary>
        public Event()
		{
            InitializeEvent();
		}

		/// <summary>
		/// Base contructor to re-instantiate an existing event
		/// </summary>
        /// <param name="serializationData">Serialized event data</param>
        public Event(byte[] serializationData)
		{
            InitializeEvent();

            Deserialize(serializationData);
		}

		/// <summary>
		/// Initializes a new event object
		/// </summary>
        private void InitializeEvent()
		{
            eventType = Guid.Empty;
			eventVersion = new Version(baseVersion);
			eventName = string.Empty;
			eventTime = 0;

			serializedEvent = new SerializationData();
		}

		/// <summary>
		/// Serializes the event and puts it in the SerializedEvent property
		/// </summary>
        /// <returns>Serialized version of the event</returns>
		public byte[] Serialize()
		{
            serializedEvent.ResetStream();

            serializedEvent.WriteMode = true;

            this.eventTime = DateTime.UtcNow.Ticks;

            GetObjectDataBase(serializedEvent);
			GetObjectData(serializedEvent);

			serializedEvent.ReadMode = true;

            return serializedEvent.ToBytes();
		}

		/// <summary>
		/// Deserializes the event
		/// </summary>
		public virtual void Deserialize( byte[] serializationData )
		{
			PropertyInfo prop;
            bool inDictionary = false;
            object dictionary = 0;
            int dictionaryCount = 0;
            object dictionaryKey = 0;

			serializedEvent = new SerializationData(serializationData);

            originatingRouterName = serializedEvent.GetOriginatingRouterName();
            inRouterName = serializedEvent.GetInRouterName();
            eventType = serializedEvent.GetEventType();

            foreach (WspKeyValuePair<string, object> kv in serializedEvent)
			{
                if (inDictionary == false)
                {
                    prop = this.GetType().GetProperty(kv.Key, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

                    if (prop != null)
                    {
                        try
                        {
                            prop.SetValue(this, kv.ValueIn, null);
                        }
                        catch
                        {
                            if (prop.PropertyType.FullName.StartsWith(@"System.Collections.Generic.Dictionary") == true)
                            {
                                inDictionary = true;
                                dictionaryCount = (int)kv.ValueIn;

                                dictionary = Activator.CreateInstance(prop.PropertyType);
                                prop.SetValue(this, dictionary, null);
                            }
                        }
                    }
                }
                else
                {
                    if (kv.Key == "key")
                    {
                        dictionaryKey = kv.ValueIn;
                    }
                    else
                    {
                        ((IDictionary)dictionary).Add(dictionaryKey, kv.ValueIn);
                        dictionaryCount--;

                        if (dictionaryCount == 0)
                        {
                            inDictionary = false;
                        }
                    }
                }
			}
		}

		/// <summary>
		/// Used for event serialization.
		/// </summary>
		/// <param name="data">SerializationData object passed to store serialized object</param>
		private void GetObjectDataBase( SerializationData data )
		{
			data.AddPrefix(OriginatingRouterName);
            data.AddPrefix(InRouterName);
            data.AddPrefix(EventType);
			data.AddElement(@"EventBaseVersion", baseVersion);
			data.AddElement(@"EventType", eventType);
			data.AddElement(@"EventVersion", eventVersion);
			data.AddElement(@"EventName", eventName);
			data.AddElement(@"EventTime", eventTime);
			data.AddElement(@"EventPublisher", System.AppDomain.CurrentDomain.FriendlyName);
		}

		/// <summary>
		/// Used for event serialization.
		/// </summary>
		/// <param name="data">SerializationData object passed to store serialized object</param>
		abstract public void GetObjectData( SerializationData data );

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
        protected virtual void Dispose(bool disposing) 
        {
            if (!disposed)
            {
                if (serializedEvent != null)
                {
                    serializedEvent.Dispose();
                }

                disposed = true;

                if (disposing)
                {
                    GC.SuppressFinalize(this);
                }
            }
        }

        /// <summary>
        /// Destructor for the Event class.
        /// </summary>
        ~Event()
        {
            Dispose(false);
        }
	}
}
