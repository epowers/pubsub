using System;
using System.Collections.Generic;
using System.Text;
using System.Web;
using System.Net;
using System.Security.Cryptography;
using Microsoft.WebSolutionsPlatform.Common;
using Microsoft.WebSolutionsPlatform.Event;

namespace Microsoft.WebSolutionsPlatform.PubSubManager
{
    /// <summary>
    /// The Subscription class defines the subscription objects which are published 
    /// when an application subscribes to event types.
    /// </summary>
	public class Subscription : WspBody
	{
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

        private string methodBody;
        /// <summary>
        /// Method body for filtered subscription
        /// </summary>
        public string MethodBody
        {
            get
            {
                return methodBody;
            }
            set
            {
                methodBody = value;
            }
        }

        private List<string> usingLibraries;
        /// <summary>
        /// Array of using libraries, if any, for filtered subscription
        /// </summary>
        public List<string> UsingLibraries
        {
            get
            {
                return usingLibraries;
            }
            set
            {
                usingLibraries = value;
            }
        }

        private List<string> referencedAssemblies;
        /// <summary>
        /// Array of referenced assemblies, if any, for filtered subscription
        /// </summary>
        public List<string> ReferencedAssemblies
        {
            get
            {
                return referencedAssemblies;
            }
            set
            {
                referencedAssemblies = value;
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
        public Subscription() :
            base()
        {
		}

        /// <summary>
        /// Base constructor to create a new subscription event
        /// </summary>
        /// <param name="serializationData">Serialized Subscription event</param>
        public Subscription(byte[] serializationData) :
            base(serializationData)
        {
        }

        /// <summary>
        /// Method called when serializing the object
        /// </summary>
        /// <param name="buffer"></param>
        public override void GetObjectData(WspBuffer buffer)
        {
            if (MethodBody == null)
            {
                MethodBody = string.Empty;
            }

            if (UsingLibraries == null)
            {
                UsingLibraries = new List<string>();
            }

            if (ReferencedAssemblies == null)
            {
                ReferencedAssemblies = new List<string>();
            }

            buffer.AddElement(@"1", SubscriptionEventType);
            buffer.AddElement(@"2", MethodBody);
            buffer.AddElement(@"3", UsingLibraries);
            buffer.AddElement(@"4", ReferencedAssemblies);
            buffer.AddElement(@"5", Subscribe);
            buffer.AddElement(@"6", LocalOnly);
        }

        /// <summary>
        /// Set values on object during deserialization
        /// </summary>
        /// <param name="elementName">Name of property</param>
        /// <param name="elementValue">Value of property</param>
        /// <returns></returns>
        public override bool SetElement(string elementName, object elementValue)
        {
            switch (elementName)
            {
                case "1":
                    SubscriptionEventType = (Guid)elementValue;
                    break;

                case "2":
                    MethodBody = (string)elementValue;
                    break;

                case "3":
                    UsingLibraries = (List<string>)elementValue;
                    break;

                case "4":
                    ReferencedAssemblies = (List<string>)elementValue;
                    break;

                case "5":
                    Subscribe = (bool)elementValue;
                    break;

                case "6":
                    LocalOnly = (bool)elementValue;
                    break;

                default:
                    base.SetElement(elementName, elementValue);
                    break;
            }

            return true;
        }
    }
}
