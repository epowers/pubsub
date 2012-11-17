using System;
using System.Collections.Generic;
using System.Text;
using System.Web;
using System.Net;
using System.Security.Cryptography;

namespace Microsoft.WebSolutionsPlatform.PubSubManager
{
    /// <summary>
    /// The Subscription class defines the subscription objects which are published 
    /// when an application subscribes to event types.
    /// </summary>
	public class Subscription
	{
		private Guid subscriptionEventType;
		/// <summary>
		/// Event registering/unregistering for
		/// </summary>
        public Guid SubscriptionEventType
		{
			get
			{
                return subscriptionEventType;
			}
			set
			{
                subscriptionEventType = value;
			}
		}

        internal byte[] subscriptionEventTypeEncodedPriv = null;
        internal byte[] subscriptionEventTypeEncoded
        {
            get
            {
                if (subscriptionEventTypeEncodedPriv == null)
                {
                    UTF8Encoding uniEncoding = new UTF8Encoding();
                    subscriptionEventTypeEncodedPriv = uniEncoding.GetBytes(SubscriptionEventType.ToString());
                }

                return subscriptionEventTypeEncodedPriv;
            }
        }

        private bool subscribe;
        /// <summary>
        /// Subscribe is true; Unsubscribe is false
        /// </summary>
        public bool Subscribe
        {
            get
            {
                return subscribe;
            }
            set
            {
                subscribe = value;
            }
        }

		private bool localOnly;
		/// <summary>
		/// Register for the event only on the local machine
		/// </summary>
		public bool LocalOnly
		{
			get
			{
				return localOnly;
			}
			set
			{
				localOnly = value;
			}
		}

		/// <summary>
		/// Base constructor to create a new subscription event
		/// </summary>
        public Subscription()
		{
            subscribe = true;
		}

        /// <summary>
        /// Base constructor to create a new subscription event
        /// </summary>
        /// <param name="serializedEvent">Serialized Subscription event</param>
        public Subscription(byte[] serializedEvent)
        {
            int position = 0;

            int valueLength = 0;

            valueLength = WspEvent.Read(serializedEvent, ref position);
            SubscriptionEventType = new Guid(WspEvent.Read(serializedEvent, ref position, valueLength));

            if (serializedEvent[position] == (byte)1)
            {
                Subscribe = true;
            }
            else
            {
                Subscribe = false;
            }

            position++;

            if (serializedEvent[position] == (byte)1)
            {
                LocalOnly = true;
            }
            else
            {
                LocalOnly = false;
            }
        }

        /// <summary>
        /// Serializes the event
        /// </summary>
        /// <returns>Serialized version of the event</returns>
        public byte[] Serialize()
        {
            int position = 0;

            byte[] buffer = new byte[subscriptionEventTypeEncoded.Length + sizeof(Int32) + 2];

            Buffer.BlockCopy(BitConverter.GetBytes((Int32)subscriptionEventTypeEncoded.Length), 0, buffer, position, sizeof(Int32));
            position += sizeof(Int32);

            Buffer.BlockCopy(subscriptionEventTypeEncoded, 0, buffer, position, subscriptionEventTypeEncoded.Length);

            position += subscriptionEventTypeEncoded.Length;

            if (Subscribe == true)
            {
                buffer[position] = (byte)1;
            }
            else
            {
                buffer[position] = (byte)0;
            }

            position++;

            if (LocalOnly == true)
            {
                buffer[position] = (byte)1;
            }
            else
            {
                buffer[position] = (byte)0;
            }

            return buffer;
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
        }
    }
}
