using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration.Install;
using System.Resources;
using System.Diagnostics;
using System.Reflection;
using System.IO;

namespace Microsoft.WebSolutionsPlatform.Event
{
	[RunInstaller(true)]
	public partial class ProjectInstaller : Installer
	{
        /// <summary>
        /// Constructor
        /// </summary>
		public ProjectInstaller()
		{
            InitializeComponent();
		}

        /// <summary>
        /// Override 'Uninstall' method of Installer class.
        /// </summary>
        /// <param name="mySavedState"></param>
        public override void Uninstall(IDictionary mySavedState)
        {
            if (mySavedState == null)
            {
                Console.WriteLine("Uninstallation Error !");
            }
            else
            {
                PerformanceCounterSetup pcSetup = new PerformanceCounterSetup();
                pcSetup.Init("uninstall");

                base.Uninstall(mySavedState);
            }
        }

        private void serviceInstaller_AfterInstall(object sender, InstallEventArgs e)
        {
            string initComplete = Context.Parameters["initComplete"];

            if (initComplete == null || initComplete == string.Empty)
            {
                PerformanceCounterSetup pcSetup = new PerformanceCounterSetup();
                pcSetup.Init("install");

                WriteOutConfig();

                Context.Parameters["initComplete"] = "true";
            }
        }

        private void WriteOutConfig()
        {
            string targetDir = Context.Parameters["TARGETDIR"];

            string wspParent = Context.Parameters["WSPPARENT"];
            string wspInternetFacing = Context.Parameters["WSPINTERNETFACING"];
            string wspEventQueueSize = Context.Parameters["WSPQUEUESIZE"];
            string wspParentUi = Context.Parameters["WSPPARENTUI"];
            string wspInternetFacingUi = Context.Parameters["WSPINTERNETFACINGUI"];
            string wspEventQueueSizeUi = Context.Parameters["WSPQUEUESIZEUI"];

            string configPath = targetDir + "WspEventRouter.exe.config";

            if (wspParentUi != string.Empty)
            {
                wspParent = wspParentUi;
            }

            if (wspEventQueueSize == string.Empty)
            {
                wspEventQueueSize = wspEventQueueSizeUi;
            }

            if (wspInternetFacing == string.Empty)
            {
                wspInternetFacing = wspInternetFacingUi;
            }

            using (StreamWriter sw = new StreamWriter(configPath))
            {
                sw.WriteLine("<?xml version=\"1.0\" encoding=\"utf-8\" ?>");
                sw.WriteLine("<configuration>");
                sw.WriteLine("	<configSections>");
                sw.WriteLine("		<section name=\"eventRouterSettings\" type=\"foo\"/>");
                sw.WriteLine("		<section name=\"eventPersistSettings\" type=\"foo2\"/>");
                sw.WriteLine("	</configSections>");
                sw.WriteLine("");
                sw.WriteLine("	<eventRouterSettings>");
                sw.WriteLine("		<!-- refreshIncrement should be about 1/3 of what the expirationIncrement is. -->");
                sw.WriteLine("		<!-- This setting needs to be consistent across all the machines in the eventing network. -->");
                sw.WriteLine("		<subscriptionManagement refreshIncrement=\"3\"  expirationIncrement=\"10\"/>");
                sw.WriteLine("");

                if (wspEventQueueSize == null || wspEventQueueSize == string.Empty)
                {
                    sw.WriteLine("		<localPublish eventQueueName=\"WspEventQueue\" eventQueueSize=\"102400000\" averageEventSize=\"10240\"/>");
                }
                else
                {
                    sw.WriteLine("		<localPublish eventQueueName=\"WspEventQueue\" eventQueueSize=\"" + wspEventQueueSize + "\" averageEventSize=\"10240\"/>");
                }

                sw.WriteLine("");
                sw.WriteLine("		<!-- These settings control what should happen to an output queue when communications is lost to a parent or child.-->");
                sw.WriteLine("		<!-- maxQueueSize is in bytes and maxTimeout is in seconds.-->");
                sw.WriteLine("		<!-- When the maxQueueSize is reached or the maxTimeout is reached for a communication that has been lost, the queue is deleted.-->");
                sw.WriteLine("		<outputCommunicationQueues maxQueueSize=\"200000000\" maxTimeout=\"600\"/>");
                sw.WriteLine("");
                sw.WriteLine("		<!-- nic can be an alias which specifies a specific IP address or an IP address. -->");
                sw.WriteLine("		<!-- port can be 0 if you don't want to have the router open a listening port to be a parent to other routers. -->");

                if (wspInternetFacing == null || wspInternetFacing == string.Empty)
                {
                    sw.WriteLine("		<thisRouter nic=\"\" port=\"1300\" bufferSize=\"1024000\" timeout=\"30000\" />");
                }
                else
                {
                    sw.WriteLine("		<thisRouter nic=\"\" port=\"0\" bufferSize=\"1024000\" timeout=\"30000\" />");
                }

                sw.WriteLine("");

                if (wspParent == null || wspParent == string.Empty)
                {
                    sw.WriteLine("		<!-- <parentRouter name=\"ParentMachineName\" port=\"1300\" bufferSize=\"1024000\" timeout=\"30000\" />  -->");
                }
                else
                {
                    sw.WriteLine("		<parentRouter name=\"" + wspParent + "\" port=\"1300\" bufferSize=\"1024000\" timeout=\"30000\" />");
                }

                sw.WriteLine("");
                sw.WriteLine("	</eventRouterSettings>");
                sw.WriteLine("");
                sw.WriteLine("	<eventPersistSettings>");
                sw.WriteLine("");
                sw.WriteLine("	    <!-- type specifies the EventType to be persisted.-->");
                sw.WriteLine("		<!-- localOnly is a boolean which specifies whether only events published on this machine are persisted or if events from the entire network are persisted.-->");
                sw.WriteLine("		<!-- maxFileSize specifies the maximum size in bytes that the persisted file should be before it is copied.-->");
                sw.WriteLine("		<!-- maxCopyInterval specifies in seconds the longest time interval before the persisted file is copied.-->");
                sw.WriteLine("		<!-- fieldTerminator specifies the character used between fields.-->");
                sw.WriteLine("		<!-- rowTerminator specifies the character used at the end of each row written.-->");
                sw.WriteLine("		<!-- tempFileDirectory is the local directory used for writing out the persisted event serializedEvent.-->");
                sw.WriteLine("		<!-- copyToFileDirectory is the final destination of the persisted serializedEvent file. It can be local or remote using a UNC.-->");
                sw.WriteLine("");
                sw.WriteLine("		<!-- <event type=\"78422526-7B21-4559-8B9A-BC551B46AE34\" localOnly=\"true\" maxFileSize=\"2000000000\" maxCopyInterval=\"60\" fieldTerminator=\",\" rowTerminator=\"\\n\" tempFileDirectory=\"c:\\temp\\WebEvents\\\" copyToFileDirectory=\"c:\\temp\\WebEvents\\log\\\" /> -->");
                sw.WriteLine("");
                sw.WriteLine("	</eventPersistSettings>");
                sw.WriteLine("</configuration>");
            }
        }
    }

    internal class PerformanceCounterSetup
    {
        internal static string categoryName;
        internal static string communicationCategoryName;
        internal static string categoryHelp;
        internal static string communicationCategoryHelp;
        internal static string subscriptionQueueSizeName;
        internal static string rePublisherQueueSizeName;
        internal static string persisterQueueSizeName;
        internal static string forwarderQueueSizeName;
        internal static string subscriptionEntriesName;
        internal static string eventsProcessedName;
        internal static string eventsProcessedBytesName;
        internal static string baseInstance;

        internal static PerformanceCounter subscriptionQueueSize;
        internal static PerformanceCounter rePublisherQueueSize;
        internal static PerformanceCounter persisterQueueSize;
        internal static PerformanceCounter forwarderQueueSize;
        internal static PerformanceCounter subscriptionEntries;
        internal static PerformanceCounter eventsProcessed;
        internal static PerformanceCounter eventsProcessedBytes;

        internal void Init(string arg)
        {
            CounterCreationDataCollection CCDC;

            categoryName = "WspEventRouter";
            communicationCategoryName = "WspEventRouterCommunication";
            categoryHelp = "WspEventRouter counters showing internal performance of the router.";
            communicationCategoryHelp = "WspEventRouter counters showing communication queues to other machines";
            subscriptionQueueSizeName = "SubscriptionQueueSize";
            rePublisherQueueSizeName = "RePublisherQueueSize";
            persisterQueueSizeName = "PersisterQueueSize";
            forwarderQueueSizeName = "ForwarderQueueSize";
            subscriptionEntriesName = "SubscriptionEntries";
            eventsProcessedName = "EventsProcessed";
            eventsProcessedBytesName = "EventsProcessedBytes";
            baseInstance = "WspEventRouter";

            if (PerformanceCounterCategory.Exists(categoryName) == true)
            {
                PerformanceCounterCategory.Delete(categoryName);
            }

            if (EventLog.SourceExists("WspEventRouter") == true)
            {
                EventLog.DeleteEventSource("WspEventRouter");
            }

            if (arg == "uninstall")
            {
                return;
            }

            EventLog.CreateEventSource("WspEventRouter", "System");

            CCDC = new CounterCreationDataCollection();

            CounterCreationData subscriptionQueueCounter = new CounterCreationData();
            subscriptionQueueCounter.CounterType = PerformanceCounterType.NumberOfItems32;
            subscriptionQueueCounter.CounterName = subscriptionQueueSizeName;
            CCDC.Add(subscriptionQueueCounter);

            CounterCreationData rePublisherQueueCounter = new CounterCreationData();
            rePublisherQueueCounter.CounterType = PerformanceCounterType.NumberOfItems32;
            rePublisherQueueCounter.CounterName = rePublisherQueueSizeName;
            CCDC.Add(rePublisherQueueCounter);

            CounterCreationData persisterQueueCounter = new CounterCreationData();
            persisterQueueCounter.CounterType = PerformanceCounterType.NumberOfItems32;
            persisterQueueCounter.CounterName = persisterQueueSizeName;
            CCDC.Add(persisterQueueCounter);

            CounterCreationData subscriptionEntriesCounter = new CounterCreationData();
            subscriptionEntriesCounter.CounterType = PerformanceCounterType.NumberOfItems32;
            subscriptionEntriesCounter.CounterName = subscriptionEntriesName;
            CCDC.Add(subscriptionEntriesCounter);

            CounterCreationData eventsProcessedCounter = new CounterCreationData();
            eventsProcessedCounter.CounterType = PerformanceCounterType.RateOfCountsPerSecond32;
            eventsProcessedCounter.CounterName = eventsProcessedName;
            CCDC.Add(eventsProcessedCounter);

            CounterCreationData eventsProcessedBytesCounter = new CounterCreationData();
            eventsProcessedBytesCounter.CounterType = PerformanceCounterType.RateOfCountsPerSecond64;
            eventsProcessedBytesCounter.CounterName = eventsProcessedBytesName;
            CCDC.Add(eventsProcessedBytesCounter);

            PerformanceCounterCategory.Create(categoryName, categoryHelp,
                PerformanceCounterCategoryType.SingleInstance, CCDC);

            subscriptionQueueSize = new PerformanceCounter(categoryName, subscriptionQueueSizeName, string.Empty, false);
            rePublisherQueueSize = new PerformanceCounter(categoryName, rePublisherQueueSizeName, string.Empty, false);
            persisterQueueSize = new PerformanceCounter(categoryName, persisterQueueSizeName, string.Empty, false);
            subscriptionEntries = new PerformanceCounter(categoryName, subscriptionEntriesName, string.Empty, false);
            eventsProcessed = new PerformanceCounter(categoryName, eventsProcessedName, string.Empty, false);
            eventsProcessedBytes = new PerformanceCounter(categoryName, eventsProcessedBytesName, string.Empty, false);

            if (PerformanceCounterCategory.Exists(communicationCategoryName) == false)
            {
                CCDC = new CounterCreationDataCollection();

                CounterCreationData forwarderQueueCounter = new CounterCreationData();
                forwarderQueueCounter.CounterType = PerformanceCounterType.NumberOfItems32;
                forwarderQueueCounter.CounterName = forwarderQueueSizeName;
                CCDC.Add(forwarderQueueCounter);

                PerformanceCounterCategory.Create(communicationCategoryName, communicationCategoryHelp,
                    PerformanceCounterCategoryType.MultiInstance, CCDC);
            }

            forwarderQueueSize = new PerformanceCounter(communicationCategoryName, forwarderQueueSizeName, baseInstance, false);

            subscriptionQueueSize.RawValue = 0;
            rePublisherQueueSize.RawValue = 0;
            persisterQueueSize.RawValue = 0;
            forwarderQueueSize.RawValue = 0;
            subscriptionEntries.RawValue = 0;
            eventsProcessed.RawValue = 0;
            eventsProcessedBytes.RawValue = 0;

        }
    }
}