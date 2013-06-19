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
using System.CodeDom.Compiler;
using Microsoft.WebSolutionsPlatform.PubSubManager;

namespace Microsoft.WebSolutionsPlatform.Router
{
    public partial class Router : ServiceBase
    {
        internal class RouteDetail
        {
            internal Dictionary<string, DateTime> Routes;

            internal RouteDetail()
            {
                Routes = new Dictionary<string, DateTime>(StringComparer.CurrentCultureIgnoreCase);
            }
        }

        internal class FilterSummary
        {
            internal Dictionary<string, FilterDetail> Filters;

            internal FilterSummary()
            {
                Filters = new Dictionary<string, FilterDetail>(StringComparer.Ordinal);
            }
        }

        internal class FilterDetail
        {
            internal Subscription subscription;
            internal WspFilterMethod filterMethod;
            internal RouteDetail UniqueRoutes;
            internal RouteDetail GlobalRoutes;

            internal FilterDetail()
            {
                UniqueRoutes = new RouteDetail();
                GlobalRoutes = new RouteDetail();
            }
        }

        internal class SubscriptionMgr : ServiceThread
        {
            internal static object subscriptionsLock = new object();
            internal static Dictionary<Guid, RouteDetail> generalSubscriptions = new Dictionary<Guid, RouteDetail>();
            internal static Dictionary<Guid, FilterSummary> filteredSubscriptions = new Dictionary<Guid, FilterSummary>();

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
                                    lock (subscriptionsLock)
                                    {
                                        if (string.IsNullOrEmpty(subscriptionEvent.MethodBody) == true)
                                        {
                                            RouteDetail routeDetail;

                                            if (generalSubscriptions.TryGetValue(subscriptionEvent.SubscriptionEventType, out routeDetail) == false)
                                            {
                                                routeDetail = new RouteDetail();
                                                routeDetail.Routes[element.WspEvent.InRouterName] = DateTime.UtcNow.AddMinutes(subscriptionManagement.ExpirationIncrement);

                                                generalSubscriptions[subscriptionEvent.SubscriptionEventType] = routeDetail;

                                                subscriptionEntries.Increment();

                                                FilterSummary filterSummary;

                                                if (filteredSubscriptions.TryGetValue(subscriptionEvent.SubscriptionEventType, out filterSummary) == true)
                                                {
                                                    foreach (string filter in filterSummary.Filters.Keys)
                                                    {
                                                        if (filterSummary.Filters[filter].UniqueRoutes.Routes.ContainsKey(element.WspEvent.InRouterName) == true)
                                                        {
                                                            filterSummary.Filters[filter].GlobalRoutes.Routes[element.WspEvent.InRouterName] =
                                                                filterSummary.Filters[filter].UniqueRoutes.Routes[element.WspEvent.InRouterName];

                                                            filterSummary.Filters[filter].UniqueRoutes.Routes.Remove(element.WspEvent.InRouterName);
                                                        }
                                                    }
                                                }
                                            }
                                            else
                                            {
                                                routeDetail.Routes[element.WspEvent.InRouterName] = DateTime.UtcNow.AddMinutes(subscriptionManagement.ExpirationIncrement);
                                            }
                                        }
                                        else
                                        {
                                            FilterSummary filterSummary;
                                            FilterDetail filterDetail;

                                            if (filteredSubscriptions.TryGetValue(subscriptionEvent.SubscriptionEventType, out filterSummary) == false)
                                            {
                                                filterSummary = new FilterSummary();
                                                filterDetail = new FilterDetail();
                                                filterSummary.Filters[subscriptionEvent.MethodBody] = filterDetail;

                                                CompilerResults results;

                                                if (WspEventObservable.CompileFilterMethod(subscriptionEvent.MethodBody, subscriptionEvent.UsingLibraries,
                                                    subscriptionEvent.ReferencedAssemblies, out filterDetail.filterMethod, out results) == false)
                                                {
                                                    continue;
                                                }

                                                filterDetail.subscription = subscriptionEvent;

                                                subscriptionEntries.Increment();

                                                filteredSubscriptions[subscriptionEvent.SubscriptionEventType] = filterSummary;
                                            }
                                            else
                                            {
                                                if (filterSummary.Filters.TryGetValue(subscriptionEvent.MethodBody, out filterDetail) == false)
                                                {
                                                    filterDetail = new FilterDetail();

                                                    CompilerResults results;

                                                    if (WspEventObservable.CompileFilterMethod(subscriptionEvent.MethodBody, subscriptionEvent.UsingLibraries,
                                                        subscriptionEvent.ReferencedAssemblies, out filterDetail.filterMethod, out results) == false)
                                                    {
                                                        continue;
                                                    }

                                                    filterDetail.subscription = subscriptionEvent;

                                                    subscriptionEntries.Increment();

                                                    filterSummary.Filters[subscriptionEvent.MethodBody] = filterDetail;
                                                }
                                            }

                                            if (generalSubscriptions.ContainsKey(subscriptionEvent.SubscriptionEventType) == true)
                                            {
                                                filterDetail.GlobalRoutes.Routes[element.WspEvent.InRouterName] =
                                                    DateTime.UtcNow.AddMinutes(subscriptionManagement.ExpirationIncrement);
                                            }
                                            else
                                            {
                                                filterDetail.UniqueRoutes.Routes[element.WspEvent.InRouterName] =
                                                    DateTime.UtcNow.AddMinutes(subscriptionManagement.ExpirationIncrement);
                                            }
                                        }
                                    }

                                    forwarderQueue.Enqueue(element);
                                }
                            }
                        }

                        if (subscriptionEntries.RawValue > 0 && DateTime.UtcNow > nextTimeout)
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
                Dictionary<Guid, Dictionary<string, List<string>>> expiredGlobalFilterSubscriptions = new Dictionary<Guid, Dictionary<string, List<string>>>();
                Dictionary<Guid, Dictionary<string, List<string>>> expiredUniqueFilterSubscriptions = new Dictionary<Guid, Dictionary<string, List<string>>>();

                foreach (Guid subscriptionEventType in generalSubscriptions.Keys)
                {
                    foreach (string inRouterName in generalSubscriptions[subscriptionEventType].Routes.Keys)
                    {
                        if (generalSubscriptions[subscriptionEventType].Routes[inRouterName] <= DateTime.UtcNow)
                        {
                            if (expiredSubscriptions.ContainsKey(subscriptionEventType) == false)
                            {
                                expiredSubscriptions[subscriptionEventType] = new List<string>();
                            }

                            expiredSubscriptions[subscriptionEventType].Add(inRouterName);
                        }
                    }
                }

                foreach (Guid subscriptionEventType in expiredSubscriptions.Keys)
                {
                    foreach (string InRouterName in expiredSubscriptions[subscriptionEventType])
                    {
                        generalSubscriptions[subscriptionEventType].Routes.Remove(InRouterName);
                    }

                    if (generalSubscriptions[subscriptionEventType].Routes.Count == 0)
                    {
                        generalSubscriptions.Remove(subscriptionEventType);

                        subscriptionEntries.Decrement();
                    }
                }

                foreach (Guid subscriptionEventType in filteredSubscriptions.Keys)
                {
                    foreach (string filter in filteredSubscriptions[subscriptionEventType].Filters.Keys)
                    {
                        foreach (string inRouterName in filteredSubscriptions[subscriptionEventType].Filters[filter].GlobalRoutes.Routes.Keys)
                        {
                            if (filteredSubscriptions[subscriptionEventType].Filters[filter].GlobalRoutes.Routes[inRouterName] <= DateTime.UtcNow)
                            {
                                if (expiredGlobalFilterSubscriptions.ContainsKey(subscriptionEventType) == false)
                                {
                                    expiredGlobalFilterSubscriptions[subscriptionEventType] = new Dictionary<string, List<string>>();
                                }

                                if (expiredGlobalFilterSubscriptions[subscriptionEventType].ContainsKey(filter) == false)
                                {
                                    expiredGlobalFilterSubscriptions[subscriptionEventType][filter] = new List<string>();
                                }

                                expiredGlobalFilterSubscriptions[subscriptionEventType][filter].Add(inRouterName);
                            }
                            else
                            {
                                if (generalSubscriptions.ContainsKey(subscriptionEventType) == true &&
                                    generalSubscriptions[subscriptionEventType].Routes.ContainsKey(inRouterName) == false)
                                {
                                    filteredSubscriptions[subscriptionEventType].Filters[filter].UniqueRoutes.Routes[inRouterName] =
                                       filteredSubscriptions[subscriptionEventType].Filters[filter].GlobalRoutes.Routes[inRouterName];

                                    filteredSubscriptions[subscriptionEventType].Filters[filter].GlobalRoutes.Routes.Remove(inRouterName);
                                }
                            }
                        }
                    }
                }

                foreach (Guid subscriptionEventType in filteredSubscriptions.Keys)
                {
                    foreach (string filter in filteredSubscriptions[subscriptionEventType].Filters.Keys)
                    {
                        foreach (string inRouterName in filteredSubscriptions[subscriptionEventType].Filters[filter].UniqueRoutes.Routes.Keys)
                        {
                            if (filteredSubscriptions[subscriptionEventType].Filters[filter].UniqueRoutes.Routes[inRouterName] <= DateTime.UtcNow)
                            {
                                if (expiredUniqueFilterSubscriptions.ContainsKey(subscriptionEventType) == false)
                                {
                                    expiredUniqueFilterSubscriptions[subscriptionEventType] = new Dictionary<string, List<string>>();
                                }

                                if (expiredUniqueFilterSubscriptions[subscriptionEventType].ContainsKey(filter) == false)
                                {
                                    expiredUniqueFilterSubscriptions[subscriptionEventType][filter] = new List<string>();
                                }

                                expiredUniqueFilterSubscriptions[subscriptionEventType][filter].Add(inRouterName);
                            }
                        }
                    }
                }

                foreach (Guid subscriptionEventType in expiredGlobalFilterSubscriptions.Keys)
                {
                    foreach (string filter in expiredGlobalFilterSubscriptions[subscriptionEventType].Keys)
                    {
                        foreach (string InRouterName in expiredGlobalFilterSubscriptions[subscriptionEventType][filter])
                        {
                            filteredSubscriptions[subscriptionEventType].Filters[filter].GlobalRoutes.Routes.Remove(InRouterName);
                        }

                        if (filteredSubscriptions[subscriptionEventType].Filters[filter].GlobalRoutes.Routes.Count == 0 &&
                            filteredSubscriptions[subscriptionEventType].Filters[filter].UniqueRoutes.Routes.Count == 0)
                        {
                            filteredSubscriptions[subscriptionEventType].Filters.Remove(filter);

                            subscriptionEntries.Decrement();
                        }

                        if (filteredSubscriptions[subscriptionEventType].Filters.Count == 0)
                        {
                            filteredSubscriptions.Remove(subscriptionEventType);
                        }
                    }
                }

                foreach (Guid subscriptionEventType in expiredUniqueFilterSubscriptions.Keys)
                {
                    foreach (string filter in expiredUniqueFilterSubscriptions[subscriptionEventType].Keys)
                    {
                        foreach (string InRouterName in expiredUniqueFilterSubscriptions[subscriptionEventType][filter])
                        {
                            filteredSubscriptions[subscriptionEventType].Filters[filter].UniqueRoutes.Routes.Remove(InRouterName);
                        }

                        if (filteredSubscriptions[subscriptionEventType].Filters[filter].GlobalRoutes.Routes.Count == 0 &&
                            filteredSubscriptions[subscriptionEventType].Filters[filter].UniqueRoutes.Routes.Count == 0)
                        {
                            filteredSubscriptions[subscriptionEventType].Filters.Remove(filter);

                            subscriptionEntries.Decrement();
                        }

                        if (filteredSubscriptions[subscriptionEventType].Filters.Count == 0)
                        {
                            filteredSubscriptions.Remove(subscriptionEventType);
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
                    foreach (Guid subscriptionEventType in generalSubscriptions.Keys)
                    {
                        foreach (string inRouterName in generalSubscriptions[subscriptionEventType].Routes.Keys)
                        {
                            if (string.Compare(inRouterName, outRouterName, true) != 0)
                            {
                                extendedHeaders[(byte)HeaderType.OriginatingRouter] = inRouterName;

                                Subscription subscription = new Subscription();

                                subscription.LocalOnly = false;
                                subscription.Subscribe = true;
                                subscription.SubscriptionEventType = subscriptionEventType;

                                WspEvent wspEvent = new WspEvent(Subscription.SubscriptionEvent, extendedHeaders, subscription.Serialize());

                                QueueElement element = new QueueElement();

                                element.WspEvent = wspEvent;
                                element.Source = EventSource.FromLocal;
                                element.BodyEvent = subscription;

                                Communicator.socketQueues[outRouterName].Enqueue(element);

                                break;
                            }
                        }
                    }

                    foreach (Guid subscriptionEventType in filteredSubscriptions.Keys)
                    {
                        foreach (string filter in filteredSubscriptions[subscriptionEventType].Filters.Keys)
                        {
                            foreach (string inRouterName in filteredSubscriptions[subscriptionEventType].Filters[filter].GlobalRoutes.Routes.Keys)
                            {
                                if (string.Compare(inRouterName, outRouterName, true) != 0)
                                {
                                    extendedHeaders[(byte)HeaderType.OriginatingRouter] = inRouterName;

                                    Subscription subscription = new Subscription();

                                    subscription.LocalOnly = false;
                                    subscription.Subscribe = true;
                                    subscription.SubscriptionEventType = subscriptionEventType;
                                    subscription.MethodBody = filteredSubscriptions[subscriptionEventType].Filters[filter].subscription.MethodBody;
                                    subscription.UsingLibraries = filteredSubscriptions[subscriptionEventType].Filters[filter].subscription.UsingLibraries;
                                    subscription.ReferencedAssemblies = filteredSubscriptions[subscriptionEventType].Filters[filter].subscription.ReferencedAssemblies;

                                    WspEvent wspEvent = new WspEvent(Subscription.SubscriptionEvent, extendedHeaders, subscription.Serialize());

                                    QueueElement element = new QueueElement();

                                    element.WspEvent = wspEvent;
                                    element.Source = EventSource.FromLocal;
                                    element.BodyEvent = subscription;

                                    Communicator.socketQueues[outRouterName].Enqueue(element);

                                    break;
                                }
                            }

                            foreach (string inRouterName in filteredSubscriptions[subscriptionEventType].Filters[filter].UniqueRoutes.Routes.Keys)
                            {
                                if (string.Compare(inRouterName, outRouterName, true) != 0)
                                {
                                    extendedHeaders[(byte)HeaderType.OriginatingRouter] = inRouterName;

                                    Subscription subscription = new Subscription();

                                    subscription.LocalOnly = false;
                                    subscription.Subscribe = true;
                                    subscription.SubscriptionEventType = subscriptionEventType;
                                    subscription.MethodBody = filteredSubscriptions[subscriptionEventType].Filters[filter].subscription.MethodBody;
                                    subscription.UsingLibraries = filteredSubscriptions[subscriptionEventType].Filters[filter].subscription.UsingLibraries;
                                    subscription.ReferencedAssemblies = filteredSubscriptions[subscriptionEventType].Filters[filter].subscription.ReferencedAssemblies;

                                    WspEvent wspEvent = new WspEvent(Subscription.SubscriptionEvent, extendedHeaders, subscription.Serialize());

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