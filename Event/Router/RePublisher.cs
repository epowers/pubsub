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
using Microsoft.WebSolutionsPlatform.Event.PubSubManager;
using Microsoft.WebSolutionsPlatform.Common;

namespace Microsoft.WebSolutionsPlatform.Event
{
	public partial class Router : ServiceBase
	{
		internal class RePublisher : ServiceThread
		{
			public override void Start()
			{
                PublishManager pubMgr = null;
				QueueElement element;
                QueueElement defaultElement = default(QueueElement);
                QueueElement newElement = new QueueElement();
				bool elementRetrieved;

                try
                {
                    Thread.Sleep(1000);

                    while (true)
                    {
                        try
                        {
                            pubMgr = new PublishManager((uint)Router.thisTimeout);

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
                            try
                            {
                                pubMgr.Publish(element.SerializedEvent);
                            }
                            catch
                            {
                                rePublisherQueue.Enqueue(element);
                            }
                        }
                    }
                }

                catch (ThreadAbortException e)
                {
                    throw e;
                }

                catch (Exception e)
                {
                    EventLog.WriteEntry("WspEventRouter", e.ToString(), EventLogEntryType.Warning);
                }
            }
		}
	}
}
