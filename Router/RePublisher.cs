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
using Microsoft.WebSolutionsPlatform.Common;

namespace Microsoft.WebSolutionsPlatform.Router
{
	public partial class Router : ServiceBase
	{
		internal class RePublisher : ServiceThread
		{
			public override void Start()
			{
                WspEventPublish eventPush = null;
				QueueElement element;
                QueueElement newElement = new QueueElement();
				bool elementRetrieved;

                try
                {
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
                        try
                        {
                            if (hubRole == true)
                            {
                                eventPush = new WspEventPublish((uint)configSettings.HubRoleSettings.ThisRouter.Timeout);
                            }
                            else
                            {
                                eventPush = new WspEventPublish((uint)configSettings.NodeRoleSettings.ParentRouter.Timeout);
                            }


                            break;
                        }
                        catch (SharedQueueException)
                        {
                            Thread.Sleep(10000);
                        }
                    }

                    while (true)
                    {
                        try
                        {
                            element = rePublisherQueue.Dequeue();

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
                            try
                            {
                                eventPush.OnNext(element.WspEvent);
                            }
                            catch
                            {
                                rePublisherQueue.Enqueue(element);
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

                catch (Exception e)
                {
                    EventLog.WriteEntry("WspEventRouter", e.ToString(), EventLogEntryType.Warning);
                }
            }
		}
	}
}
