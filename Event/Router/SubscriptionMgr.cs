using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Diagnostics;
using System.ServiceProcess;
using System.Threading;
using System.Text;
using System.Reflection;
using System.Reflection.Emit;
using System.Xml.XPath;

namespace Microsoft.WebSolutionsPlatform.Event
{
	public partial class Router : ServiceBase
	{
		internal class SubscriptionEntry : IComparable<SubscriptionEntry>
		{
            private Guid subscriptionId;
			/// <summary>
			/// ID for subscription
			/// </summary>
            public Guid SubscriptionId
			{
				get
				{
                    return subscriptionId;
				}
			}

			private string routerName;
			/// <summary>
			/// RouterName where subscription is made
			/// </summary>
			public string RouterName
			{
				get
				{
					return routerName;
				}
			}

            private Guid eventType;
			/// <summary>
			/// Event type registering for
			/// </summary>
            public Guid EventType
			{
				get
				{
                    return eventType;
				}
			}

			private bool localOnly;
			/// <summary>
			/// Defines if the subscription is only for the local machine
			/// </summary>
			public bool LocalOnly
			{
				get
				{
					return localOnly;
				}
			}

			private DateTime expirationTime;
			/// <summary>
			/// Expiration time for subscription
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

			private QueueElement eventQueueElement;
			/// <summary>
			/// EventQueueElement for subscription
			/// </summary>
			public QueueElement EventQueueElement
			{
				get
				{
					return eventQueueElement;
				}

				internal set
				{
					eventQueueElement = value;
				}
			}

			/// <summary>
			/// Used to create a SubscriptionEntry used by RouteMgr
			/// </summary>
            /// <param name="subscriptionId">ID of the subscription</param>
            /// <param name="eventType">event being registered for the subscription</param>
			/// <param name="routerName">routerName where subscription is made</param>
			/// <param name="localOnly">Defines if subscription is for local machine only</param>
            public SubscriptionEntry(Guid subscriptionId, Guid eventType, string routerName, bool localOnly)
			{
                this.subscriptionId = subscriptionId;
                this.eventType = eventType;
				this.routerName = routerName;
				this.localOnly = localOnly;
				this.expirationTime = DateTime.UtcNow.AddMinutes(5);
			}

			public int CompareTo( SubscriptionEntry otherSubscription )
			{
                return subscriptionId.CompareTo(otherSubscription.subscriptionId);
			}
		}

		internal class SubscriptionMgr : ServiceThread
		{
            internal static Dictionary<Guid, SubscriptionEntry> subscriptions = new Dictionary<Guid, SubscriptionEntry>();

            private DateTime nextTimeout = DateTime.UtcNow.AddMinutes(subscriptionExpirationIncrement);
            private DateTime nextPushSubscriptions = DateTime.UtcNow.AddMinutes(subscriptionRefreshIncrement);

			public SubscriptionMgr()
			{
			}

			public override void Start()
			{
				QueueElement element;
                QueueElement defaultElement = default(QueueElement);
                QueueElement newElement = new QueueElement();

                newElement.OriginatingRouterName = string.Empty;
                newElement.InRouterName = string.Empty;

				Subscription subscriptionEvent;

				bool elementRetrieved;

                string inRouterName;

                try
                {
                    Manager.ThreadInitialize.Release();
                }
                catch
                {
                    // If the thread is restarted, this could throw an exception but just ignore
                }

                try
                {
                    while (true)
                    {
                        try
                        {
                            element = subscriptionMgrQueue.Dequeue();

                            if (element.Equals(defaultElement) == true)
                            {
                                element = newElement;
                                elementRetrieved = false;
                            }
                            else
                            {
                                elementRetrieved = true;
                            }
                        }
                        catch (InvalidOperationException)
                        {
                            element = newElement;
                            elementRetrieved = false;
                        }

                        if (elementRetrieved == true)
                        {
                            if (element.Event == null)
                            {
                                element.Event = new Subscription();

                                element.Event.Deserialize(element.SerializedEvent);
                            }

                            subscriptionEvent = (Subscription)element.Event;

                            if (subscriptionEvent.LocalOnly == false)
                            {
                                if (element.OriginatingRouterName.Length == 0)
                                {
                                    element.OriginatingRouterName = subscriptionEvent.OriginatingRouterName;
                                }

                                if (subscriptionEvent.Subscribe == true)
                                {
                                    if (subscriptions.ContainsKey(subscriptionEvent.SubscriptionId) == false)
                                    {
                                        if (channelDictionary.TryGetValue(element.OriginatingRouterName, out inRouterName) == false)
                                        {
                                            inRouterName = element.InRouterName;
                                        }

                                        Router.AddSubscription(subscriptionEvent.SubscriptionId, subscriptionEvent.SubscriptionEventType,
                                            inRouterName, subscriptionEvent.LocalOnly);

                                        subscriptions[subscriptionEvent.SubscriptionId].EventQueueElement = element;
                                    }

                                    subscriptions[subscriptionEvent.SubscriptionId].ExpirationTime = DateTime.UtcNow.AddMinutes(subscriptionExpirationIncrement);
                                }
                                else
                                {
                                    Router.DeleteSubscription(subscriptionEvent.SubscriptionId);
                                    subscriptions.Remove(subscriptionEvent.SubscriptionId);
                                }

                                forwarderQueue.Enqueue(element);
                            }
                        }

                        if (subscriptions.Count > 0 && DateTime.UtcNow > nextTimeout)
                        {
                            RemoveExpiredEntries();

                            nextTimeout = DateTime.UtcNow.AddMinutes(subscriptionExpirationIncrement);
                        }
                    }
                }
                catch
                {
                    // intentionally left blank
                }
			}

            private static void RemoveExpiredEntries()
			{
				List<Guid> expiredSubscriptions = new List<Guid>();

				foreach(Guid subId in subscriptions.Keys)
				{
                    if (subscriptions[subId].ExpirationTime <= DateTime.UtcNow)
					{
						expiredSubscriptions.Add(subId);
					}
				}

				for(int i = 0; i < expiredSubscriptions.Count; i++)
				{
					Router.DeleteSubscription(expiredSubscriptions[i]);
				}
			}

            /// <summary>
            /// Resend all subscriptions. This is intended to be used after a connection is made.
            /// </summary>
            public static void ResendSubscriptions()
            {
                foreach (SubscriptionEntry subEntry in subscriptions.Values)
                {
                    forwarderQueue.Enqueue(subEntry.EventQueueElement);
                }
            }
        }
	}
}