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

namespace Microsoft.WebSolutionsPlatform.Router
{
	public partial class Router : ServiceBase
	{
		internal class Manager : ServiceThread
		{
            public static Semaphore ThreadInitialize = new Semaphore(0, 1);

            static RequestListenMgr requestListener;
            
			public override void Start()
			{
                string servicePrefix;
                char[] sep = new Char[] { '/' };

                try
                {
                    Type[] workerThreadTypes = new Type[workerThreads.Count];
                    workerThreads.Keys.CopyTo(workerThreadTypes, 0);

                    if (string.Compare(configSettings.EventRouterSettings.Role, @"hub", true) == 0 &&
                        string.Compare(configSettings.EventRouterSettings.BootstrapUrl, @"GetConfig", true) == 0)
                    {
                        servicePrefix = @"http://*:80/" + configSettings.EventRouterSettings.BootstrapUrl;
                        servicePrefix = servicePrefix.TrimEnd(sep);
                        servicePrefix = servicePrefix + @"/";

                        requestListener = new RequestListenMgr();
                        RequestListenMgr.SetServicePrefix(servicePrefix);
                    }
                    else
                    {
                        requestListener = null;
                    }

                    while (configSettings.EventRouterSettings.AutoConfig == true && configSettings.EventRouterSettings.MgmtGuid == Guid.Empty)
                    {
                        Thread.Sleep(10000);

                        try
                        {
                            Router.Configurator.LoadConfiguration();
                        }
                        catch
                        {
                        }
                    }

                    while (true)
                    {
                        try
                        {
                            foreach (Type workerThreadType in workerThreadTypes)
                            {
                                Thread workerThread = workerThreads[workerThreadType];

                                if (workerThread == null ||
                                    workerThread.ThreadState == System.Threading.ThreadState.Stopped)
                                {
                                    if (workerThread == null)
                                    {
                                        ThreadInitialize.WaitOne(5000, false);
                                    }

                                    Object obj = Activator.CreateInstance(workerThreadType);
                                    workerThreads[workerThreadType] = new Thread(new ThreadStart(((ServiceThread)obj).Start));

                                    workerThreads[workerThreadType].Start();
                                }
                            }

                            Thread.Sleep(5000);
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
                catch (ThreadAbortException)
                {
                    bool abortCompleted = false;

                    foreach (Type workerThreadType in workerThreads.Keys)
                    {
                        workerThreads[workerThreadType].Abort();
                    }

                    while (abortCompleted == false)
                    {
                        abortCompleted = true;

                        foreach (Type workerThreadType in workerThreads.Keys)
                        {
                            if (workerThreads[workerThreadType].ThreadState != System.Threading.ThreadState.Aborted)
                            {
                                abortCompleted = false;
                            }
                        }

                        if (abortCompleted == false)
                            Thread.Sleep(1000);
                    }
                }
                finally
                {
                    if (RequestListenMgr.listener != null)
                    {
                        RequestListenMgr.listener.Close();
                    }
                }
			}
		}
	}
}