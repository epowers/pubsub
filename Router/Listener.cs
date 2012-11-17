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

using Microsoft.WebSolutionsPlatform.Common;
using Microsoft.WebSolutionsPlatform.Event;
using Microsoft.WebSolutionsPlatform.PubSubManager;

namespace Microsoft.WebSolutionsPlatform.Router
{
    public partial class Router : ServiceBase
    {
        internal class Listener : ServiceThread
        {
            public override void Start()
            {
                if (eventQueue == null)
                {
                    eventQueue = new SharedQueue(localPublish.EventQueueName, localPublish.EventQueueSize, (uint)localPublish.AverageEventSize);
                }

                ListenToEvents();
            }

            public static void ListenToEvents()
            {
                EventSource eventSource;
                SocketInfo socketInfo;
                Microsoft.WebSolutionsPlatform.PubSubManager.WspEvent wspEvent;

                try
                {
                    byte[] buffer;
                    int i;

                    try
                    {
                        Manager.ThreadInitialize.Release();
                    }
                    catch
                    {
                        // If the thread is restarted, this could throw an exception but just ignore
                    }

                    while (true)
                    {
                        Thread.Sleep(0);

                        if (hubRole == true)
                        {
                            buffer = eventQueue.Dequeue((UInt32)configSettings.HubRoleSettings.ThisRouter.Timeout);
                        }
                        else
                        {
                            buffer = eventQueue.Dequeue((UInt32)configSettings.NodeRoleSettings.ParentRouter.Timeout);
                        }

                        if (buffer == null)
                        {
                            continue;
                        }

                        try
                        {
                            wspEvent = new Microsoft.WebSolutionsPlatform.PubSubManager.WspEvent(buffer);
                        }
                        catch (Exception e)
                        {
                            EventLog.WriteEntry("WspEventRouter", "Event has invalid format:  " + e.ToString(), EventLogEntryType.Error);

                            continue;
                        }

                        eventsProcessed.Increment();
                        eventsProcessedBytes.IncrementBy((long)buffer.Length);

                        if (string.Compare(wspEvent.InRouterName, LocalRouterName, true) == 0)
                        {
                            eventSource = EventSource.FromLocal;
                        }
                        else
                        {
                            if (Communicator.commSockets.TryGetValue(wspEvent.InRouterName, out socketInfo) == true)
                            {
                                if (socketInfo.Hub == true)
                                {
                                    if (string.Compare(socketInfo.Group, configSettings.EventRouterSettings.Group, true) == 0)
                                    {
                                        eventSource = EventSource.FromHub;
                                    }
                                    else
                                    {
                                        eventSource = EventSource.FromPeer;
                                    }
                                }
                                else
                                {
                                    eventSource = EventSource.FromNode;
                                }
                            }
                            else
                            {
                                eventSource = EventSource.FromLocal;
                            }
                        }

                        if (wspEvent.EventType == Subscription.SubscriptionEvent)
                        {
                            QueueElement element = new QueueElement();

                            element.WspEvent = wspEvent;
                            element.Source = eventSource;
                            element.BodyEvent = new Subscription(wspEvent.Body);

                            for (i = 0; i < 10; i++)
                            {
                                try
                                {
                                    subscriptionMgrQueue.Enqueue(element);
                                    break;
                                }
                                catch (System.TimeoutException)
                                {
                                    continue;
                                }
                            }

                            continue;
                        }

                        try
                        {
                            if (SubscriptionMgr.subscriptions.ContainsKey(wspEvent.EventType) == true)
                            {
                                QueueElement element = new QueueElement();

                                element.WspEvent = wspEvent;
                                element.Source = eventSource;

                                for (i = 0; i < 10; i++)
                                {
                                    try
                                    {
                                        forwarderQueue.Enqueue(element);
                                        break;
                                    }
                                    catch (System.TimeoutException)
                                    {
                                        continue;
                                    }
                                }
                            }
                        }
                        catch
                        {
                        }

                        if (wspEvent.EventType == configSettings.EventRouterSettings.MgmtGuid)
                        {
                            QueueElement element = new QueueElement();

                            element.WspEvent = wspEvent;
                            element.Source = eventSource;

                            for (i = 0; i < 10; i++)
                            {
                                try
                                {
                                    mgmtQueue.Enqueue(element);
                                    break;
                                }
                                catch (System.TimeoutException)
                                {
                                    continue;
                                }
                            }
                        }

                        if (wspEvent.EventType == configSettings.EventRouterSettings.CmdGuid)
                        {
                            QueueElement element = new QueueElement();

                            element.WspEvent = wspEvent;
                            element.Source = eventSource;

                            for (i = 0; i < 10; i++)
                            {
                                try
                                {
                                    cmdQueue.Enqueue(element);
                                    break;
                                }
                                catch (System.TimeoutException)
                                {
                                    continue;
                                }
                            }
                        }

                        if (Persister.persistEvents.ContainsKey(wspEvent.EventType) == true)
                        {
                            if (Persister.persistEvents[wspEvent.EventType].InUse == true)
                            {
                                QueueElement element = new QueueElement();

                                element.WspEvent = wspEvent;
                                element.Source = eventSource;

                                for (i = 0; i < 10; i++)
                                {
                                    try
                                    {
                                        persisterQueue.Enqueue(element);
                                        break;
                                    }
                                    catch (System.TimeoutException)
                                    {
                                        continue;
                                    }
                                }
                            }
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
            }
        }
    }
}
