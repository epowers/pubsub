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
using System.Xml;
using System.Xml.XPath;

namespace Microsoft.WebSolutionsPlatform.Event
{
	public partial class Router : ServiceBase
	{
        internal static void LoadConfiguration()
		{
			string configValueIn;
            string machineNameIn;
			int portIn;
			int bufferSizeIn;
			int timeoutIn;

			string configFile = AppDomain.CurrentDomain.SetupInformation.ApplicationBase +
				AppDomain.CurrentDomain.FriendlyName + ".config";

            XPathDocument document = new XPathDocument(configFile);
            XPathNavigator navigator = document.CreateNavigator();
			XPathNodeIterator iterator;

			if(File.Exists(configFile) == false)
				return;

            iterator = navigator.Select(@"/configuration/eventRouterSettings/subscriptionManagement");

            if (iterator.MoveNext() == true)
            {
                configValueIn = iterator.Current.GetAttribute(@"refreshIncrement", String.Empty);
                if (configValueIn.Length != 0)
                    subscriptionRefreshIncrement = UInt32.Parse(configValueIn);

                configValueIn = iterator.Current.GetAttribute(@"expirationIncrement", String.Empty);
                if (configValueIn.Length != 0)
                    subscriptionExpirationIncrement = UInt32.Parse(configValueIn);
            }

            iterator = navigator.Select(@"/configuration/eventRouterSettings/localPublish");

			if(iterator.MoveNext() == true)
            {
                configValueIn = iterator.Current.GetAttribute(@"eventQueueName", String.Empty);
                if (configValueIn.Length != 0)
                    eventQueueName = configValueIn;

                configValueIn = iterator.Current.GetAttribute(@"eventQueueSize", String.Empty);
                if (configValueIn.Length != 0)
                    eventQueueSize = UInt32.Parse(configValueIn);

                configValueIn = iterator.Current.GetAttribute(@"averageEventSize", String.Empty);
                if (configValueIn.Length != 0)
                    averageEventSize = Int32.Parse(configValueIn);
			}

            iterator = navigator.Select(@"/configuration/eventRouterSettings/outputCommunicationQueues");

            if (iterator.MoveNext() == true)
            {
                configValueIn = iterator.Current.GetAttribute(@"maxQueueSize", String.Empty);
                if (configValueIn.Length != 0)
                    thisOutQueueMaxSize = Int32.Parse(configValueIn);

                configValueIn = iterator.Current.GetAttribute(@"maxTimeout", String.Empty);
                if (configValueIn.Length != 0)
                    thisOutQueueMaxTimeout = Int32.Parse(configValueIn);
            }

            iterator = navigator.Select(@"/configuration/eventRouterSettings/thisRouter");

			if(iterator.MoveNext() == true)
			{
                thisNic = iterator.Current.GetAttribute(@"nic", String.Empty).Trim();

                configValueIn = iterator.Current.GetAttribute(@"port", String.Empty).Trim();
                if (configValueIn.Length == 0)
                {
                    thisPort = 0;
                }
                else
                {
                    thisPort = int.Parse(configValueIn);
                }

				configValueIn = iterator.Current.GetAttribute(@"bufferSize", String.Empty);
                if (configValueIn.Length != 0)
					thisBufferSize = int.Parse(configValueIn);

				configValueIn = iterator.Current.GetAttribute(@"timeout", String.Empty);
                if (configValueIn.Length != 0)
					thisTimeout = int.Parse(configValueIn);
			}

            iterator = navigator.Select(@"/configuration/eventRouterSettings/parentRouter");

			while(iterator.MoveNext() == true)
			{
                machineNameIn = iterator.Current.GetAttribute(@"name", String.Empty).Trim();

				portIn = int.Parse(iterator.Current.GetAttribute(@"port", String.Empty));

				configValueIn = iterator.Current.GetAttribute(@"bufferSize", String.Empty);
                if (configValueIn.Length != 0)
					bufferSizeIn = int.Parse(configValueIn);
				else
					bufferSizeIn = thisBufferSize;

				configValueIn = iterator.Current.GetAttribute(@"timeout", String.Empty);
                if (configValueIn.Length != 0)
					timeoutIn = int.Parse(configValueIn);
				else
					timeoutIn = thisTimeout;

                AddRoute(machineNameIn, portIn, bufferSizeIn, timeoutIn);
			}

            LoadPersistConfig();
		}

        internal static long GetConfigFileTick()
        {
            string configFile = AppDomain.CurrentDomain.SetupInformation.ApplicationBase +
                AppDomain.CurrentDomain.FriendlyName + ".config";

            return (new FileInfo(configFile)).LastWriteTimeUtc.Ticks;
        }

        internal static void LoadPersistConfig()
        {
            string configValueIn;
            Guid eventType;

            string configFile = AppDomain.CurrentDomain.SetupInformation.ApplicationBase +
                AppDomain.CurrentDomain.FriendlyName + ".config";

            XPathDocument document = new XPathDocument(configFile);
            XPathNavigator navigator = document.CreateNavigator();
            XPathNodeIterator iterator;

            Persister.lastConfigFileTick = GetConfigFileTick();

            iterator = navigator.Select(@"/configuration/eventPersistSettings/event");

            while (iterator.MoveNext() == true)
            {
                PersistEventInfo eventInfo;

                configValueIn = iterator.Current.GetAttribute(@"type", String.Empty);

                eventType = new Guid(configValueIn);

                if (Persister.persistEvents.TryGetValue(eventType, out eventInfo) == false)
                {
                    eventInfo = new PersistEventInfo();

                    eventInfo.OutFileName = null;
                    eventInfo.OutStream = null;
                }

                eventInfo.InUse = true;
                eventInfo.Loaded = true;

                eventInfo.PersistEventType = eventType;

                configValueIn = iterator.Current.GetAttribute(@"localOnly", String.Empty);
                if (configValueIn.Length == 0)
                {
                    eventInfo.LocalOnly = true;
                }
                else
                {
                    eventInfo.LocalOnly = bool.Parse(configValueIn);
                }
                configValueIn = iterator.Current.GetAttribute(@"maxFileSize", String.Empty);
                if (configValueIn.Length != 0)
                    eventInfo.MaxFileSize = long.Parse(configValueIn);

                configValueIn = iterator.Current.GetAttribute(@"maxCopyInterval", String.Empty);
                if (configValueIn.Length != 0)
                    eventInfo.CopyIntervalTicks = long.Parse(configValueIn) * 10000000;

                configValueIn = iterator.Current.GetAttribute(@"fieldTerminator", String.Empty);
                if (configValueIn.Length == 0)
                {
                    eventInfo.FieldTerminator = ",";
                }
                else
                {
                    eventInfo.FieldTerminator = char.Parse(configValueIn).ToString();
                }

                configValueIn = iterator.Current.GetAttribute(@"rowTerminator", String.Empty);
                if (configValueIn.Length == 0)
                {
                    eventInfo.RowTerminator = "\n";
                }
                else
                {
                    if (configValueIn == @"\n")
                    {
                        configValueIn = "\n";
                    }
                    else
                    {
                        if (configValueIn == @"\r")
                        {
                            configValueIn = "\r";
                        }
                        else
                        {
                            if (configValueIn == @"\t")
                            {
                                configValueIn = "\t";
                            }
                        }
                    }

                    eventInfo.RowTerminator = char.Parse(configValueIn).ToString();
                }

                configValueIn = iterator.Current.GetAttribute(@"tempFileDirectory", String.Empty);
                if (configValueIn.Length == 0)
                {
                    configValueIn = @"C:\temp\" + Guid.NewGuid().ToString() + @"\";
                }

                eventInfo.TempFileDirectory = configValueIn;

                if (Directory.Exists(configValueIn) == false)
                    Directory.CreateDirectory(configValueIn);

                configValueIn = iterator.Current.GetAttribute(@"copyToFileDirectory", String.Empty);
                if (configValueIn.Length == 0)
                {
                    configValueIn = eventInfo.TempFileDirectory + @"log\";
                }

                eventInfo.CopyToFileDirectory = configValueIn;

                if (Directory.Exists(configValueIn) == false)
                    Directory.CreateDirectory(configValueIn);

                configValueIn = configValueIn + @"temp\";

                if (Directory.Exists(configValueIn) == false)
                    Directory.CreateDirectory(configValueIn);

                Persister.persistEvents[eventType] = eventInfo;
            }
        }
	}
}
