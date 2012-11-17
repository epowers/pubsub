using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration.Install;
using System.Resources;
using System.Diagnostics;
using System.Reflection;
using System.IO;

namespace Microsoft.WebSolutionsPlatform.Router
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

            string wspRole = Context.Parameters["ROLE"];
            string wspRoleUI = Context.Parameters["ROLEUI"];
            string wspBootstrapUrl = Context.Parameters["BOOTSTRAPURL"];
            string wspMgmtGuid = Context.Parameters["MGMTGUID"];
            string wspMgmtGuidUi = Context.Parameters["MGMTGUIDUI"];
            string wspCmdGuid = Context.Parameters["CMDGUID"];
            string wspCmdGuidUi = Context.Parameters["CMDGUIDUI"];
            string wspGroup = Context.Parameters["GROUP"];
            string wspGroupUi = Context.Parameters["GROUPUI"];

            string configPath = targetDir + "WspEventRouter.exe.config";

            if (string.IsNullOrEmpty(wspRoleUI) == false)
            {
                wspRole = "Hub";
            }
            else
            {
                if (string.IsNullOrEmpty(wspRole) == true)
                {
                    wspRole = "Node";
                }
            }

            if (string.Compare(wspRole, "Node", true) != 0 && string.Compare(wspRole, "Hub", true) != 0)
            {
                wspRole = "Node";
            }

            if (string.IsNullOrEmpty(wspGroup) == true && string.IsNullOrEmpty(wspGroupUi) == false)
            {
                wspGroup = wspGroupUi;
            }
            else
            {
                if (string.IsNullOrEmpty(wspGroup) == true)
                {
                    if (Router.LocalRouterName.Length >= 3)
                    {
                        wspGroup = Router.LocalRouterName.Substring(0, 3).ToLower();
                    }
                    else
                    {
                        wspGroup = string.Empty;
                    }
                }
            }

            if (string.IsNullOrEmpty(wspMgmtGuid) == true && string.IsNullOrEmpty(wspMgmtGuidUi) == false)
            {
                wspMgmtGuid = wspMgmtGuidUi;
            }
            else
            {
                if (string.IsNullOrEmpty(wspMgmtGuid) == true)
                {
                    wspMgmtGuid = "DA761E42-69DD-45b0-BDE7-C500A6A0DA0E";
                }
            }

            try
            {
                Guid testGuid = new Guid(wspMgmtGuid);
            }
            catch
            {
                wspMgmtGuid = "DA761E42-69DD-45b0-BDE7-C500A6A0DA0E";
            }

            if (string.IsNullOrEmpty(wspCmdGuid) == true && string.IsNullOrEmpty(wspCmdGuidUi) == false)
            {
                wspCmdGuid = wspCmdGuidUi;
            }
            else
            {
                if (string.IsNullOrEmpty(wspCmdGuid) == true)
                {
                    wspMgmtGuid = "345CF9E9-F206-4572-8451-0F519C739A7E";
                }
            }

            try
            {
                Guid testGuid = new Guid(wspCmdGuid);
            }
            catch
            {
                wspCmdGuid = "345CF9E9-F206-4572-8451-0F519C739A7E";
            }

            
            using (StreamWriter sw = new StreamWriter(configPath))
            {
                sw.WriteLine("<?xml version=\"1.0\" encoding=\"utf-8\" ?>");
                sw.WriteLine("<configuration>");
                sw.WriteLine("	<configSections>");
                sw.WriteLine("		<section name=\"eventRouterSettings\" type=\"Microsoft.WebSolutionsPlatform.Configuration.EventRouterSettings\"/>");
                sw.WriteLine("		<section name=\"hubRoleSettings\" type=\"Microsoft.WebSolutionsPlatform.Configuration.HubRoleSettings\"/>");
                sw.WriteLine("		<section name=\"nodeRoleSettings\" type=\"Microsoft.WebSolutionsPlatform.Configuration.HubRoleSettings\"/>");
                sw.WriteLine("		<section name=\"groupSettings\" type=\"Microsoft.WebSolutionsPlatform.Configuration.GroupSettings\"/>");
                sw.WriteLine("		<section name=\"logSettings\" type=\"Microsoft.WebSolutionsPlatform.Configuration.LogSettings\"/>");
                sw.WriteLine("	</configSections>");
                sw.WriteLine("");
                sw.WriteLine("    <!-- role = {hub, node} -->");
                sw.WriteLine("    <!-- group = <name> -->");
                sw.WriteLine("    <!-- autoConfig = {true, false} -->");
                sw.WriteLine("    <!-- mgmtGuid = <GUID> -->");
                sw.WriteLine("    <!-- cmdGuid = <GUID> -->");
                sw.WriteLine("    <!-- publish = {true, false}  **Only ONE of the hub servers should be configured to publish. This server will be master for the global config file. -->");
                sw.WriteLine("    <!-- bootstrapUrl = <URL> **This will be called to retrieve the config file if mgmtGuid does not exist or if the file is corrupt. -->");
                sw.WriteLine("");

                if (string.IsNullOrEmpty(wspBootstrapUrl) == true)
                {
                    sw.WriteLine("	<eventRouterSettings role=\"" + wspRole + "\" group=\"" + wspGroup + "\" autoConfig=\"true\" mgmtGuid=\"" + wspMgmtGuid + "\" cmdGuid=\"" + wspCmdGuid + "\" publish=\"false\"/>");
                }
                else
                {
                    sw.WriteLine("	<eventRouterSettings role=\"" + wspRole + "\" group=\"" + wspGroup + "\" autoConfig=\"true\" bootstrapUrl=\"" + wspBootstrapUrl + "\" />");
                }

                sw.WriteLine("");
                sw.WriteLine("    <!-- refreshIncrement should be about 1/3 of what the expirationIncrement is. -->");
                sw.WriteLine("    <!-- This setting needs to be consistent across all the machines in the eventing network. -->");
                sw.WriteLine("    <!-- <subscriptionManagement refreshIncrement=\"3\"  expirationIncrement=\"10\"/> -->");
                sw.WriteLine("");
                sw.WriteLine("    <!-- <localPublish eventQueueName=\"WspEventQueue\" eventQueueSize=\"102400000\" averageEventSize=\"10240\"/> -->");
                sw.WriteLine("");
                sw.WriteLine("    <!-- These settings control what should happen to an output queue when communications is lost to a parent or child.-->");
                sw.WriteLine("    <!-- maxQueueSize is in bytes and maxTimeout is in seconds.-->");
                sw.WriteLine("    <!-- When the maxQueueSize is reached or the maxTimeout is reached for a communication that has been lost, the queue is deleted.-->");
                sw.WriteLine("    <!-- <outputCommunicationQueues maxQueueSize=\"200000000\" maxTimeout=\"600\"/> -->");
                sw.WriteLine("");
                sw.WriteLine("    <!-- nic can be an alias which specifies a specific IP address or an IP address. -->");
                sw.WriteLine("    <!-- port can be 0 if you don't want to have the router open a listening port to be a parent to other routers. -->");
                sw.WriteLine("    <!-- <thisRouter nic=\"\" port=\"1300\" bufferSize=\"1024000\" timeout=\"30000\" /> -->");
                sw.WriteLine("");
                sw.WriteLine("    <hubRoleSettings>");
                sw.WriteLine("      <subscriptionManagement refreshIncrement=\"3\"  expirationIncrement=\"10\"/>");
                sw.WriteLine("		<localPublish eventQueueName=\"WspEventQueue\" eventQueueSize=\"102400000\" averageEventSize=\"10240\"/>");
                sw.WriteLine("		<outputCommunicationQueues maxQueueSize=\"200000000\" maxTimeout=\"600\"/>");
                sw.WriteLine("		<thisRouter nic=\"\" port=\"1300\" bufferSize=\"1024000\" timeout=\"5000\" />");
                sw.WriteLine("		<peerRouter numConnections=\"2\" port=\"1300\" bufferSize=\"1024000\" timeout=\"5000\" />");
                sw.WriteLine("    </hubRoleSettings>");
                sw.WriteLine("");
                sw.WriteLine("    <nodeRoleSettings>");
                sw.WriteLine("      <subscriptionManagement refreshIncrement=\"3\"  expirationIncrement=\"10\"/>");
                sw.WriteLine("		<localPublish eventQueueName=\"WspEventQueue\" eventQueueSize=\"10240000\" averageEventSize=\"10240\"/>");
                sw.WriteLine("		<outputCommunicationQueues maxQueueSize=\"200000000\" maxTimeout=\"600\"/>");
                sw.WriteLine("		<parentRouter numConnections=\"1\" port=\"1300\" bufferSize=\"1024000\" timeout=\"5000\" />");
                sw.WriteLine("    </nodeRoleSettings>");
                sw.WriteLine("");
                sw.WriteLine("    <groupSettings>");
                sw.WriteLine("      <group name=\"" + wspGroup + "\" useGroup=\"\">");

                if (string.Compare(wspRole, "Hub", true) == 0)
                {
                    sw.WriteLine("        <hub name=\"" + Router.LocalRouterName + "\"/>");
                }

                sw.WriteLine("      </group>");
                sw.WriteLine("");
                sw.WriteLine("      <group name=\"Grp2\" useGroup=\"" + wspGroup + "\">");
                sw.WriteLine("      </group>");
                sw.WriteLine("");
                sw.WriteLine("      <group name=\"default\" useGroup=\"" + wspGroup + "\">");
                sw.WriteLine("      </group>");
                sw.WriteLine("    </groupSettings>");


                sw.WriteLine("");
                sw.WriteLine("	<logSettings>");
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
                sw.WriteLine("    <!-- <event type=\"78422526-7B21-4559-8B9A-BC551B46AE34\" localOnly=\"true\" maxFileSize=\"2000000000\" maxCopyInterval=\"60\" createEmptyFiles=\"false\" fieldTerminator=\",\" rowTerminator=\"\\n\" tempFileDirectory=\"c:\\temp\\WebEvents\\\" copyToFileDirectory=\"c:\\temp\\WebEvents\\log\\\" /> -->");
                sw.WriteLine("");
                sw.WriteLine("	</logSettings>");
                sw.WriteLine("");
                sw.WriteLine("</configuration>");
            }
        }
    }

    /// <summary>
    /// This exposes the class to manually install and remove the performance counter categories which need to be 
    /// done if the msi is not used to install the application
    /// </summary>
    public class PerformanceCounterSetup
    {
        internal static string categoryName;
        internal static string communicationCategoryName;
        internal static string categoryHelp;
        internal static string communicationCategoryHelp;
        internal static string subscriptionQueueSizeName;
        internal static string rePublisherQueueSizeName;
        internal static string persisterQueueSizeName;
        internal static string forwarderQueueSizeName;
        internal static string mgmtQueueSizeName;
        internal static string cmdQueueSizeName;
        internal static string subscriptionEntriesName;
        internal static string eventsProcessedName;
        internal static string eventsProcessedBytesName;
        internal static string baseInstance;

        internal static PerformanceCounter subscriptionQueueSize;
        internal static PerformanceCounter rePublisherQueueSize;
        internal static PerformanceCounter persisterQueueSize;
        internal static PerformanceCounter forwarderQueueSize;
        internal static PerformanceCounter mgmtQueueSize;
        internal static PerformanceCounter cmdQueueSize;
        internal static PerformanceCounter subscriptionEntries;
        internal static PerformanceCounter eventsProcessed;
        internal static PerformanceCounter eventsProcessedBytes;

        /// <summary>
        /// Installs/Uninstalls the perforance counters used by the application
        /// </summary>
        /// <param name="arg">Argument should be "uninstall" to remove the performance counters</param>
        public void Init(string arg)
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
            mgmtQueueSizeName = "MgmtQueueSizeName";
            cmdQueueSizeName = "CmdQueueSizeName";
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

            CounterCreationData mgmtQueueCounter = new CounterCreationData();
            mgmtQueueCounter.CounterType = PerformanceCounterType.NumberOfItems32;
            mgmtQueueCounter.CounterName = mgmtQueueSizeName;
            CCDC.Add(mgmtQueueCounter);

            CounterCreationData cmdQueueCounter = new CounterCreationData();
            cmdQueueCounter.CounterType = PerformanceCounterType.NumberOfItems32;
            cmdQueueCounter.CounterName = cmdQueueSizeName;
            CCDC.Add(cmdQueueCounter);

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
            mgmtQueueSize = new PerformanceCounter(categoryName, mgmtQueueSizeName, string.Empty, false);
            cmdQueueSize = new PerformanceCounter(categoryName, cmdQueueSizeName, string.Empty, false);
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
            mgmtQueueSize.RawValue = 0;
            cmdQueueSize.RawValue = 0;
            subscriptionEntries.RawValue = 0;
            eventsProcessed.RawValue = 0;
            eventsProcessedBytes.RawValue = 0;

        }
    }
}