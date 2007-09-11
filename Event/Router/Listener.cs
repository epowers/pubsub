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

namespace Microsoft.WebSolutionsPlatform.Event
{
	public partial class Router : ServiceBase
	{
		internal class Listener : ServiceThread
		{
            internal static PrefixStream prefixStream = new PrefixStream();

			public override void Start()
			{
                if (eventQueue == null)
                {
                    eventQueue = new SharedQueue(eventQueueName, eventQueueSize, (uint)averageEventSize);
                }

                ListenToEvents();
			}

            public static void ListenToEvents()
			{
                Guid eventType;
                string originatingRouterName;
                string inRouterName;

				DoubleDictionary<string, Guid> eventCheck;

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
                        buffer = eventQueue.Dequeue((UInt32) thisTimeout);

                        if (buffer == null)
                        {
                            continue;
                        }

                        lock (prefixStream)
                        {
                            prefixStream.Position = 0;

                            Buffer.BlockCopy(buffer, 0, prefixStream.prefixBuffer, 0,
                                buffer.Length < prefixStream.prefixLength ? buffer.Length : prefixStream.prefixLength);

                            originatingRouterName = prefixStream.prefixReader.ReadString();
                            inRouterName = prefixStream.prefixReader.ReadString();
                            eventType = new Guid(prefixStream.prefixReader.ReadString());
                        }

                        eventsProcessed.Increment();
                        eventsProcessedBytes.IncrementBy((long)buffer.Length);

                        if (eventType == Event.SubscriptionEvent)
                        {
                            QueueElement element = new QueueElement();

                            element.SerializedEvent = buffer;
                            element.SerializedLength = buffer.Length;
                            element.EventType = eventType;
                            element.OriginatingRouterName = originatingRouterName;
                            element.InRouterName = inRouterName;

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

                        eventDictionary.TryGetValue(Guid.Empty, out eventCheck);

                        if (eventCheck.Dictionary1 != null)
                        {
                            if (eventCheck.Dictionary1.Count > 0)
                            {
                                QueueElement element = new QueueElement();

                                element.SerializedEvent = buffer;
                                element.SerializedLength = buffer.Length;
                                element.EventType = eventType;
                                element.OriginatingRouterName = originatingRouterName;
                                element.InRouterName = inRouterName;

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
                        else
                        {
                            eventDictionary.TryGetValue(eventType, out eventCheck);

                            if (eventCheck.Dictionary1 != null)
                            {
                                if (eventCheck.Dictionary1.Count > 0)
                                {
                                    QueueElement element = new QueueElement();

                                    element.SerializedEvent = buffer;
                                    element.SerializedLength = buffer.Length;
                                    element.EventType = eventType;
                                    element.OriginatingRouterName = originatingRouterName;
                                    element.InRouterName = inRouterName;

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
                        }

                        if (Persister.persistEvents.ContainsKey(eventType) == true)
                        {
                            QueueElement element = new QueueElement();

                            element.SerializedEvent = buffer;
                            element.SerializedLength = buffer.Length;
                            element.EventType = eventType;
                            element.OriginatingRouterName = originatingRouterName;
                            element.InRouterName = inRouterName;

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
                        else
                        {
                            if (Persister.persistAllEvents == true)
                            {
                                QueueElement element = new QueueElement();

                                element.SerializedEvent = buffer;
                                element.SerializedLength = buffer.Length;
                                element.EventType = eventType;
                                element.OriginatingRouterName = originatingRouterName;
                                element.InRouterName = inRouterName;

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
