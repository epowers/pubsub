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
			Guid eventType;

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

            iterator = navigator.Select(@"/configuration/eventRouterSettings/thisRouter");

			if(iterator.MoveNext() == true)
			{
				configValueIn = iterator.Current.GetAttribute(@"port", String.Empty);
                if (configValueIn.Length != 0)
					thisPort = int.Parse(configValueIn);

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
                machineNameIn = iterator.Current.GetAttribute(@"name", String.Empty);

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

            iterator = navigator.Select(@"/configuration/eventPersistSettings/event");

			while(iterator.MoveNext() == true)
			{
                PersistEventInfo eventInfo = new PersistEventInfo();
                eventInfo.OutFileName = null;
                eventInfo.OutStream = null;

                configValueIn = iterator.Current.GetAttribute(@"type", String.Empty);
                if (configValueIn == @"*" || configValueIn.Length == 0)
				{
					eventType = Guid.Empty;
                    Persister.persistAllEvents = true;
                }
				else
				{
                    eventType = new Guid(configValueIn);
				}

                configValueIn = iterator.Current.GetAttribute(@"localOnly", String.Empty);
                if (configValueIn.Length == 0)
				{
                    eventInfo.LocalOnly = true;
				}
				else
				{
                    eventInfo.LocalOnly = bool.Parse(configValueIn);
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
