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
using Microsoft.WebSolutionsPlatform.PubSubManager;

namespace Microsoft.WebSolutionsPlatform.Router
{
    public partial class Router : ServiceBase
    {
        internal class SubscriptionDetail
        {
            internal object SubscriptionDetailLock;
            internal Dictionary<string, DateTime> Routes;

            internal SubscriptionDetail()
            {
                SubscriptionDetailLock = new object();
                Routes = new Dictionary<string, DateTime>(StringComparer.CurrentCultureIgnoreCase);
            }
        }

        internal class SubscriptionMgr : ServiceThread
        {
            internal static object subscriptionsLock = new object();
            internal static Dictionary<Guid, SubscriptionDetail> subscriptions = new Dictionary<Guid, SubscriptionDetail>();

            private DateTime nextTimeout = DateTime.UtcNow.AddMinutes(subscriptionManagement.ExpirationIncrement);
            private DateTime nextPushSubscriptions = DateTime.UtcNow.AddMinutes(subscriptionManagement.RefreshIncrement);

            public SubscriptionMgr()
            {
            }

            public override void Start()
            {
                QueueElement element;
                QueueElement newElement = new QueueElement();

                Subscription subscriptionEvent;

                bool elementRetrieved;

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

                            if (element == default(QueueElement))
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
                            if (element.BodyEvent == null)
                            {
                                element.BodyEvent = new Subscription(element.WspEvent.Body);
                            }

                            subscriptionEvent = (Subscription)element.BodyEvent;

                            if (subscriptionEvent.LocalOnly == false)
                            {
                                if (subscriptionEvent.Subscribe == true)
                                {
                                    SubscriptionDetail subscriptionDetail;

                                    if (subscriptions.TryGetValue(subscriptionEvent.SubscriptionEventType, out subscriptionDetail) == false)
                                    {
                                        lock (subscriptionsLock)
                                        {
                                            if (subscriptions.TryGetValue(subscriptionEvent.SubscriptionEventType, out subscriptionDetail) == false)
                                            {
                                                subscriptionDetail = new SubscriptionDetail();
                                                subscriptionDetail.Routes[element.WspEvent.InRouterName] = DateTime.UtcNow.AddMinutes(subscriptionManagement.ExpirationIncrement);

                                                subscriptions[subscriptionEvent.SubscriptionEventType] = subscriptionDetail;

                                                subscriptionEntries.Increment();

                                            }
                                            else
                                            {
                                                lock (subscriptionDetail.SubscriptionDetailLock)
                                                {
                                                    subscriptionDetail.Routes[element.WspEvent.InRouterName] = DateTime.UtcNow.AddMinutes(subscriptionManagement.ExpirationIncrement);
                                                }
                                            }
                                        }
                                    }
                                    else
                                    {
                                        lock (subscriptionDetail.SubscriptionDetailLock)
                                        {
                                            subscriptionDetail.Routes[element.WspEvent.InRouterName] = DateTime.UtcNow.AddMinutes(subscriptionManagement.ExpirationIncrement);
                                        }
                                    }

                                    forwarderQueue.Enqueue(element);
                                }
                            }
                        }

                        if (subscriptions.Count > 0 && DateTime.UtcNow > nextTimeout)
                        {
                            lock (subscriptionsLock)
                            {
                                RemoveExpiredEntries();
                            }

                            nextTimeout = DateTime.UtcNow.AddMinutes(subscriptionManagement.ExpirationIncrement);
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
                Dictionary<Guid, List<string>> expiredSubscriptions = new Dictionary<Guid, List<string>>();

                foreach (Guid subscriptionEventType in subscriptions.Keys)
                {
                    lock (subscriptions[subscriptionEventType].SubscriptionDetailLock)
                    {
                        foreach (string inRouterName in subscriptions[subscriptionEventType].Routes.Keys)
                        {
                            if (subscriptions[subscriptionEventType].Routes[inRouterName] <= DateTime.UtcNow)
                            {
                                if (expiredSubscriptions.ContainsKey(subscriptionEventType) == false)
                                {
                                    expiredSubscriptions[subscriptionEventType] = new List<string>();
                                }

                                expiredSubscriptions[subscriptionEventType].Add(inRouterName);
                            }
                        }
                    }
                }

                foreach (Guid subscriptionEventType in expiredSubscriptions.Keys)
                {
                    lock (subscriptions[subscriptionEventType].SubscriptionDetailLock)
                    {
                        foreach (string InRouterName in expiredSubscriptions[subscriptionEventType])
                        {
                            subscriptions[subscriptionEventType].Routes.Remove(InRouterName);
                        }

                        if (subscriptions[subscriptionEventType].Routes.Count == 0)
                        {
                            subscriptions.Remove(subscriptionEventType);

                            subscriptionEntries.Decrement();
                        }
                    }
                }
            }

            /// <summary>
            /// Resend all subscriptions. This is intended to be used after a connection is made.
            /// </summary>
            internal static void ResendSubscriptions(string outRouterName)
            {
                Dictionary<byte, string> extendedHeaders = new Dictionary<byte, string>();

                lock (subscriptionsLock)
                {
                    foreach (Guid subscriptionEventType in subscriptions.Keys)
                    {
                        lock (subscriptions[subscriptionEventType].SubscriptionDetailLock)
                        {
                            foreach (string inRouterName in subscriptions[subscriptionEventType].Routes.Keys)
                            {
                                if (string.Compare(inRouterName, outRouterName, true) != 0)
                                {
                                    extendedHeaders[(byte)HeaderType.OriginatingRouter] = inRouterName;

                                    Subscription subscription = new Subscription();

                                    subscription.LocalOnly = false;
                                    subscription.Subscribe = true;
                                    subscription.SubscriptionEventType = subscriptionEventType;

                                    Microsoft.WebSolutionsPlatform.PubSubManager.WspEvent wspEvent = new Microsoft.WebSolutionsPlatform.PubSubManager.WspEvent(Subscription.SubscriptionEvent, extendedHeaders, subscription.Serialize());

                                    QueueElement element = new QueueElement();

                                    element.WspEvent = wspEvent;
                                    element.Source = EventSource.FromLocal;
                                    element.BodyEvent = subscription;

                                    Communicator.socketQueues[outRouterName].Enqueue(element);

                                    break;
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}