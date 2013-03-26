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
using Microsoft.WebSolutionsPlatform.Event;
using Microsoft.WebSolutionsPlatform.PubSubManager;

namespace Microsoft.WebSolutionsPlatform.Router
{
    internal class EventRouterSettings
    {
        private string role;
        internal string Role
        {
            get
            {
                return role;
            }
            set
            {
                role = value;
            }
        }

        private string group;
        internal string Group
        {
            get
            {
                return group;
            }
            set
            {
                group = value;
            }
        }

        private byte[] groupEncoded;
        internal byte[] GroupEncoded
        {
            get
            {
                if (groupEncoded == null)
                {
                    UTF8Encoding uniEncoding = new UTF8Encoding();
                    groupEncoded = uniEncoding.GetBytes(Group);
                }

                return groupEncoded;
            }
        }

        private bool autoConfig;
        internal bool AutoConfig
        {
            get
            {
                return autoConfig;
            }
            set
            {
                autoConfig = value;
            }
        }

        private Guid mgmtGuid;
        internal Guid MgmtGuid
        {
            get
            {
                return mgmtGuid;
            }
            set
            {
                mgmtGuid = value;
            }
        }

        private Guid cmdGuid;
        internal Guid CmdGuid
        {
            get
            {
                return cmdGuid;
            }
            set
            {
                cmdGuid = value;
            }
        }

        private bool publish;
        internal bool Publish
        {
            get
            {
                return publish;
            }
            set
            {
                publish = value;
            }
        }

        private string bootstrapUrl;
        public string BootstrapUrl
        {
            get
            {
                return bootstrapUrl;
            }
            set
            {
                bootstrapUrl = value;
            }
        }

        internal EventRouterSettings()
        {
            role = "node";
            group = "default";
            autoConfig = true;
            mgmtGuid = Guid.Empty;
            cmdGuid = Guid.Empty;
            publish = false;
            bootstrapUrl = "GetConfig";
        }
    }

    internal class SubscriptionManagement
    {
        private int refreshIncrement;
        public int RefreshIncrement
        {
            get
            {
                return refreshIncrement;
            }
            set
            {
                refreshIncrement = value;
            }
        }

        private int expirationIncrement;
        public int ExpirationIncrement
        {
            get
            {
                return expirationIncrement;
            }
            set
            {
                expirationIncrement = value;
            }
        }

        internal SubscriptionManagement()
        {
            refreshIncrement = 3;
            expirationIncrement = 10;
        }
    }

    internal class LocalPublish
    {
        private string eventQueueName;
        public string EventQueueName
        {
            get
            {
                return eventQueueName;
            }
            set
            {
                eventQueueName = value;
            }
        }

        private UInt32 eventQueueSize;
        public UInt32 EventQueueSize
        {
            get
            {
                return eventQueueSize;
            }
            set
            {
                eventQueueSize = value;
            }
        }

        private int averageEventSize;
        public int AverageEventSize
        {
            get
            {
                return averageEventSize;
            }
            set
            {
                averageEventSize = value;
            }
        }

        internal LocalPublish()
        {
            eventQueueName = "WspEventQueue";
            eventQueueSize = 10240000;
            averageEventSize = 10240;
        }
    }

    internal class OutputCommunicationQueues
    {
        private int maxQueueSize;
        public int MaxQueueSize
        {
            get
            {
                return maxQueueSize;
            }
            set
            {
                maxQueueSize = value;
            }
        }

        private int maxTimeout;
        public int MaxTimeout
        {
            get
            {
                return maxTimeout;
            }
            set
            {
                maxTimeout = value;
            }
        }

        internal OutputCommunicationQueues()
        {
            maxQueueSize = 20000000;
            maxTimeout = 600;
        }
    }

    internal class ThisRouter
    {
        private string nic;
        public string Nic
        {
            get
            {
                return nic;
            }
            set
            {
                nic = value;
            }
        }

        private int port;
        public int Port
        {
            get
            {
                return port;
            }
            set
            {
                port = value;
            }
        }

        private int bufferSize;
        public int BufferSize
        {
            get
            {
                return bufferSize;
            }
            set
            {
                bufferSize = value;
            }
        }

        private int timeout;
        public int Timeout
        {
            get
            {
                return timeout;
            }
            set
            {
                timeout = value;
            }
        }

        internal ThisRouter()
        {
            nic = string.Empty;
            port = 1300;
            bufferSize = 1024000;
            timeout = 30000;
        }
    }

    internal class PeerRouter
    {
        private int numConnections;
        public int NumConnections
        {
            get
            {
                return numConnections;
            }
            set
            {
                numConnections = value;
            }
        }

        private int port;
        public int Port
        {
            get
            {
                return port;
            }
            set
            {
                port = value;
            }
        }

        private int bufferSize;
        public int BufferSize
        {
            get
            {
                return bufferSize;
            }
            set
            {
                bufferSize = value;
            }
        }

        private int timeout;
        public int Timeout
        {
            get
            {
                return timeout;
            }
            set
            {
                timeout = value;
            }
        }

        internal PeerRouter()
        {
            numConnections = 2;
            port = 1300;
            bufferSize = 1024000;
            timeout = 30000;
        }
    }

    internal class ParentRouter
    {
        private int numConnections;
        public int NumConnections
        {
            get
            {
                return numConnections;
            }
            set
            {
                numConnections = value;
            }
        }

        private int port;
        public int Port
        {
            get
            {
                return port;
            }
            set
            {
                port = value;
            }
        }

        private int bufferSize;
        public int BufferSize
        {
            get
            {
                return bufferSize;
            }
            set
            {
                bufferSize = value;
            }
        }

        private int timeout;
        public int Timeout
        {
            get
            {
                return timeout;
            }
            set
            {
                timeout = value;
            }
        }

        internal ParentRouter()
        {
            numConnections = 1;
            port = 1300;
            bufferSize = 1024000;
            timeout = 30000;
        }
    }

    internal class HubRoleSettings
    {
        private SubscriptionManagement subscriptionManagement;
        private LocalPublish localPublish;
        private OutputCommunicationQueues outputCommunicationQueues;
        private ThisRouter thisRouter;
        private PeerRouter peerRouter;

        internal HubRoleSettings()
        {
            subscriptionManagement = new SubscriptionManagement();
            localPublish = new LocalPublish();
            outputCommunicationQueues = new OutputCommunicationQueues();
            thisRouter = new ThisRouter();
            peerRouter = new PeerRouter();
        }

        internal SubscriptionManagement SubscriptionManagement
        {
            get { return subscriptionManagement; }
        }

        internal LocalPublish LocalPublish
        {
            get { return localPublish; }
        }

        internal OutputCommunicationQueues OutputCommunicationQueues
        {
            get { return outputCommunicationQueues; }
        }

        internal ThisRouter ThisRouter
        {
            get { return thisRouter; }
        }

        internal PeerRouter PeerRouter
        {
            get { return peerRouter; }
        }
    }

    internal class NodeRoleSettings
    {
        private SubscriptionManagement subscriptionManagement;
        private LocalPublish localPublish;
        private OutputCommunicationQueues outputCommunicationQueues;
        private ParentRouter parentRouter;

        internal NodeRoleSettings()
        {
            subscriptionManagement = new SubscriptionManagement();
            localPublish = new LocalPublish();
            outputCommunicationQueues = new OutputCommunicationQueues();
            parentRouter = new ParentRouter();
        }

        internal SubscriptionManagement SubscriptionManagement
        {
            get { return subscriptionManagement; }
        }

        internal LocalPublish LocalPublish
        {
            get { return localPublish; }
        }

        internal OutputCommunicationQueues OutputCommunicationQueues
        {
            get { return outputCommunicationQueues; }
        }

        internal ParentRouter ParentRouter
        {
            get { return parentRouter; }
        }
    }

    internal class Hub
    {
        private string name;
        internal string Name
        {
            get
            {
                return name;
            }
            set
            {
                name = value;
            }
        }

        internal Hub()
        {
            name = string.Empty;
        }
    }

    internal class Group
    {
        private string name;
        internal string Name
        {
            get
            {
                return name;
            }
            set
            {
                name = value;
            }
        }

        private byte[] nameEncoded;
        internal byte[] NameEncoded
        {
            get
            {
                if (nameEncoded == null)
                {
                    UTF8Encoding uniEncoding = new UTF8Encoding();
                    nameEncoded = uniEncoding.GetBytes(Name);
                }

                return nameEncoded;
            }
        }

        private string useGroup;
        internal string UseGroup
        {
            get
            {
                return useGroup;
            }
            set
            {
                useGroup = value;
            }
        }

        private Dictionary<string, Hub> hubs;
        internal Dictionary<string, Hub> Hubs
        {
            get
            {
                return hubs;
            }
        }

        internal Group()
        {
            name = "default";
            useGroup = string.Empty;
            hubs = new Dictionary<string, Hub>(StringComparer.CurrentCultureIgnoreCase);
        }
    }

    internal class GroupSettings
    {
        private Dictionary<string, Group> groups;
        internal Dictionary<string, Group> Groups
        {
            get
            {
                return groups;
            }
        }

        internal GroupSettings()
        {
            groups = new Dictionary<string, Group>(StringComparer.CurrentCultureIgnoreCase);
        }
    }

    internal class ConfigSettings
    {
        private EventRouterSettings eventRouterSettings;
        internal EventRouterSettings EventRouterSettings
        {
            get
            {
                return eventRouterSettings;
            }
        }

        private HubRoleSettings hubRoleSettings;
        internal HubRoleSettings HubRoleSettings
        {
            get
            {
                return hubRoleSettings;
            }
        }

        private NodeRoleSettings nodeRoleSettings;
        internal NodeRoleSettings NodeRoleSettings
        {
            get
            {
                return nodeRoleSettings;
            }
        }

        private GroupSettings groupSettings;
        internal GroupSettings GroupSettings
        {
            get
            {
                return groupSettings;
            }
        }

        internal ConfigSettings()
        {
            eventRouterSettings = new EventRouterSettings();
            hubRoleSettings = new HubRoleSettings();
            nodeRoleSettings = new NodeRoleSettings();
            groupSettings = new GroupSettings();
        }
    }

    internal class ConfigEvent : Event.WspBody
    {
        private string data;
        /// <summary>
        /// Contents of the config file
        /// </summary>
        public string Data
        {
            get
            {
                return data;
            }
            set
            {
                data = value;
            }
        }

        /// <summary>
        /// Base constructor to create a new config event
        /// </summary>
        public ConfigEvent() :
            base()
        {
        }

        /// <summary>
        /// Base constructor to create a new config event from a serialized event
        /// </summary>
        /// <param name="serializationData">Serialized event buffer</param>
        public ConfigEvent(byte[] serializationData) :
            base(serializationData)
        {
        }

        public override void GetObjectData(WspBuffer buffer)
        {
            buffer.AddElement(@"Data", data);
        }
    }

    public partial class Router : ServiceBase
    {
        internal class Configurator : ServiceThread
        {
            public override void Start()
            {
                QueueElement element;
                QueueElement defaultElement = default(QueueElement);
                QueueElement newElement = new QueueElement();
                bool elementRetrieved;
                string prevValue = string.Empty;
                long nextSendTick = DateTime.Now.Ticks;
                WspEventPublish eventPush = null;
                ConfigEvent mgmtEvent;

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

                    eventPush = new WspEventPublish();

                    mgmtEvent = new ConfigEvent();

                    while (true)
                    {
                        if (hubRole == true && configSettings.EventRouterSettings.Publish == true)
                        {
                            if (nextSendTick <= DateTime.Now.Ticks)
                            {
                                string configFile = AppDomain.CurrentDomain.SetupInformation.ApplicationBase +
                                    AppDomain.CurrentDomain.FriendlyName + ".config";

                                LoadConfiguration();

                                if (configSettings.EventRouterSettings.AutoConfig == true)
                                {
                                    mgmtEvent.Data = File.ReadAllText(configFile);
                                    mgmtEvent.EventType = configSettings.EventRouterSettings.MgmtGuid;

                                    eventPush.OnNext(new WspEvent(configSettings.EventRouterSettings.MgmtGuid, null, mgmtEvent.Serialize()));
                                }

                                nextSendTick = DateTime.Now.Ticks + 600000000L;
                            }
                        }

                        try
                        {
                            element = mgmtQueue.Dequeue();

                            if (element == defaultElement)
                            {
                                element = newElement;
                                elementRetrieved = false;
                            }
                            else
                            {
                                elementRetrieved = true;

                                forwarderQueue.Enqueue(element);
                            }
                        }
                        catch (InvalidOperationException)
                        {
                            element = newElement;
                            elementRetrieved = false;
                        }

                        if (configSettings.EventRouterSettings.AutoConfig == true &&
                            configSettings.EventRouterSettings.Publish == false &&
                            elementRetrieved == true)
                        {
                            try
                            {
                                ConfigEvent configEvent = new ConfigEvent(element.WspEvent.Body);

                                if (string.Compare(prevValue, configEvent.Data, true) != 0)
                                {
                                    SaveNewConfigFile(configSettings, configEvent.Data);

                                    LoadConfiguration();

                                    prevValue = configEvent.Data;
                                }
                            }
                            catch
                            {
                                EventLog.WriteEntry("WspEventRouter", "Could not deserialize a ConfigEvent.", EventLogEntryType.Warning);
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

            internal static void LoadConfiguration()
            {
                string configFile;
                ConfigSettings newConfigSettings = new ConfigSettings();

                lock (configFileLock)
                {
                Restart:

                    configFile = AppDomain.CurrentDomain.SetupInformation.ApplicationBase +
                         AppDomain.CurrentDomain.FriendlyName + ".config";

                    XPathDocument document = new XPathDocument(configFile);
                    XPathNavigator navigator = document.CreateNavigator();

                    if (File.Exists(configFile) == false)
                    {
                        EventLog.WriteEntry("WspEventRouter", "No config file can be found.", EventLogEntryType.Error);

                        throw new Exception("No config file found");
                    }

                    if (LoadEventRouterSettings(navigator, newConfigSettings) == true)
                    {
                        goto Restart;
                    }

                    if (string.Compare(newConfigSettings.EventRouterSettings.Role, "hub", true) != 0 &&
                        string.Compare(newConfigSettings.EventRouterSettings.Role, "node", true) != 0)
                    {
                        newConfigSettings.EventRouterSettings.Role = "node";
                    }

                    if (string.IsNullOrEmpty(newConfigSettings.EventRouterSettings.Group) == true)
                    {
                        if (Router.LocalRouterName.Length >= 3)
                        {
                            newConfigSettings.EventRouterSettings.Group = Router.LocalRouterName.Substring(0, 3).ToLower();
                        }
                    }

                    LoadHubRoleSettings(navigator, newConfigSettings);
                    LoadNodeRoleSettings(navigator, newConfigSettings);
                    LoadGroupSettings(navigator, newConfigSettings);

                    if (string.Compare(newConfigSettings.EventRouterSettings.Role, "hub", true) == 0)
                    {
                        Dictionary<string, Hub> hubs = GetHubList(newConfigSettings, newConfigSettings.EventRouterSettings.Group);

                        if(hubs.ContainsKey(Router.LocalRouterName) == false)
                        {
                            newConfigSettings.EventRouterSettings.Role = "node";
                            EventLog.WriteEntry("WspEventRouter", "Role has been changed to Node since name was not found in Hub list", EventLogEntryType.Error);
                        }
                    }

                    if (string.Compare(newConfigSettings.EventRouterSettings.Role, "hub", true) == 0)
                    {
                        hubRole = true;
                        subscriptionManagement = newConfigSettings.HubRoleSettings.SubscriptionManagement;
                        outputCommunicationQueues = newConfigSettings.HubRoleSettings.OutputCommunicationQueues;
                        localPublish = newConfigSettings.HubRoleSettings.LocalPublish;
                    }
                    else
                    {
                        hubRole = false;
                        subscriptionManagement = newConfigSettings.NodeRoleSettings.SubscriptionManagement;
                        outputCommunicationQueues = newConfigSettings.NodeRoleSettings.OutputCommunicationQueues;
                        localPublish = newConfigSettings.NodeRoleSettings.LocalPublish;
                    }

                    configSettings = newConfigSettings;

                    LoadLogSettings();
                    LoadLocalLogSettings();
                }
            }

            internal static Dictionary<string, Hub> GetHubList(ConfigSettings configSettings, string groupNameIn)
            {
                Group currGroup = null;
                string groupName = groupNameIn;

                while (currGroup == null)
                {
                    if (configSettings.GroupSettings.Groups.TryGetValue(groupName, out currGroup) == true)
                    {
                        if (string.IsNullOrEmpty(currGroup.UseGroup) == false)
                        {
                            groupName = currGroup.UseGroup;
                            currGroup = null;
                            continue;
                        }
                        else
                        {
                            continue;
                        }
                    }
                    else
                    {
                        if (string.Compare(groupName, "default", true) != 0)
                        {
                            return new Dictionary<string, Hub>();
                        }

                        groupName = "default";
                        currGroup = null;
                    }
                }

                return currGroup.Hubs;
            }

            internal static bool LoadEventRouterSettings(XPathNavigator navigator, ConfigSettings newConfigSettings)
            {
                string configValueIn;
                XPathNodeIterator iterator;

                try
                {
                    iterator = navigator.Select(@"/configuration/eventRouterSettings");

                    if (iterator.MoveNext() == true)
                    {
                        configValueIn = iterator.Current.GetAttribute(@"autoConfig", String.Empty);
                        if (configValueIn.Length != 0)
                            newConfigSettings.EventRouterSettings.AutoConfig = bool.Parse(configValueIn);

                        configValueIn = iterator.Current.GetAttribute(@"mgmtGuid", String.Empty);
                        if (configValueIn.Length != 0)
                            newConfigSettings.EventRouterSettings.MgmtGuid = new Guid(configValueIn);

                        configValueIn = iterator.Current.GetAttribute(@"cmdGuid", String.Empty);
                        if (configValueIn.Length != 0)
                            newConfigSettings.EventRouterSettings.CmdGuid = new Guid(configValueIn);
                        else
                            newConfigSettings.EventRouterSettings.CmdGuid = Guid.NewGuid();

                        configValueIn = iterator.Current.GetAttribute(@"role", String.Empty);
                        if (configValueIn.Length != 0)
                            newConfigSettings.EventRouterSettings.Role = configValueIn;

                        configValueIn = iterator.Current.GetAttribute(@"group", String.Empty);
                        if (configValueIn.Length != 0)
                            newConfigSettings.EventRouterSettings.Group = configValueIn;

                        configValueIn = iterator.Current.GetAttribute(@"publish", String.Empty);
                        if (configValueIn.Length != 0)
                            newConfigSettings.EventRouterSettings.Publish = bool.Parse(configValueIn);

                        configValueIn = iterator.Current.GetAttribute(@"bootstrapUrl", String.Empty).Trim();
                        if (configValueIn.Length != 0)
                            newConfigSettings.EventRouterSettings.BootstrapUrl = configValueIn;
                    }
                }
                catch (Exception e)
                {
                    EventLog.WriteEntry("WspEventRouter", "Error loading config file: " + e.Message, EventLogEntryType.Error);
                }

                if (newConfigSettings.EventRouterSettings.AutoConfig == true && newConfigSettings.EventRouterSettings.MgmtGuid == Guid.Empty)
                {
                    string originConfig = GetOriginConfig(newConfigSettings.EventRouterSettings.BootstrapUrl);

                    SaveNewConfigFile(newConfigSettings, originConfig);

                    newConfigSettings.EventRouterSettings.BootstrapUrl = "GetConfig";

                    return true;
                }

                return false;
            }

            internal static void LoadHubRoleSettings(XPathNavigator navigator, ConfigSettings newConfigSettings)
            {
                string configValueIn;
                XPathNodeIterator iterator;

                try
                {
                    iterator = navigator.Select(@"/configuration/hubRoleSettings/subscriptionManagement");

                    if (iterator.MoveNext() == true)
                    {
                        configValueIn = iterator.Current.GetAttribute(@"refreshIncrement", String.Empty);
                        if (configValueIn.Length != 0)
                            newConfigSettings.HubRoleSettings.SubscriptionManagement.RefreshIncrement = Int32.Parse(configValueIn);

                        configValueIn = iterator.Current.GetAttribute(@"expirationIncrement", String.Empty);
                        if (configValueIn.Length != 0)
                            newConfigSettings.HubRoleSettings.SubscriptionManagement.ExpirationIncrement = Int32.Parse(configValueIn);
                    }
                }
                catch
                {
                }

                try
                {
                    iterator = navigator.Select(@"/configuration/hubRoleSettings/localPublish");

                    if (iterator.MoveNext() == true)
                    {
                        configValueIn = iterator.Current.GetAttribute(@"eventQueueName", String.Empty);
                        if (configValueIn.Length != 0)
                            newConfigSettings.HubRoleSettings.LocalPublish.EventQueueName = configValueIn;

                        configValueIn = iterator.Current.GetAttribute(@"eventQueueSize", String.Empty);
                        if (configValueIn.Length != 0)
                            newConfigSettings.HubRoleSettings.LocalPublish.EventQueueSize = UInt32.Parse(configValueIn);

                        configValueIn = iterator.Current.GetAttribute(@"averageEventSize", String.Empty);
                        if (configValueIn.Length != 0)
                            newConfigSettings.HubRoleSettings.LocalPublish.AverageEventSize = Int32.Parse(configValueIn);
                    }
                }
                catch
                {
                }

                try
                {
                    iterator = navigator.Select(@"/configuration/hubRoleSettings/outputCommunicationQueues");

                    if (iterator.MoveNext() == true)
                    {
                        configValueIn = iterator.Current.GetAttribute(@"maxQueueSize", String.Empty);
                        if (configValueIn.Length != 0)
                            newConfigSettings.HubRoleSettings.OutputCommunicationQueues.MaxQueueSize = Int32.Parse(configValueIn);

                        configValueIn = iterator.Current.GetAttribute(@"maxTimeout", String.Empty);
                        if (configValueIn.Length != 0)
                            newConfigSettings.HubRoleSettings.OutputCommunicationQueues.MaxTimeout = Int32.Parse(configValueIn);
                    }
                }
                catch
                {
                }

                try
                {
                    iterator = navigator.Select(@"/configuration/hubRoleSettings/thisRouter");

                    if (iterator.MoveNext() == true)
                    {
                        configValueIn = iterator.Current.GetAttribute(@"nic", String.Empty).Trim();
                        if (configValueIn.Length != 0)
                            newConfigSettings.HubRoleSettings.ThisRouter.Nic = configValueIn;

                        configValueIn = iterator.Current.GetAttribute(@"port", String.Empty).Trim();
                        if (configValueIn.Length != 0)
                            newConfigSettings.HubRoleSettings.ThisRouter.Port = int.Parse(configValueIn);

                        configValueIn = iterator.Current.GetAttribute(@"bufferSize", String.Empty);
                        if (configValueIn.Length != 0)
                            newConfigSettings.HubRoleSettings.ThisRouter.BufferSize = int.Parse(configValueIn);

                        configValueIn = iterator.Current.GetAttribute(@"timeout", String.Empty);
                        if (configValueIn.Length != 0)
                            newConfigSettings.HubRoleSettings.ThisRouter.Timeout = int.Parse(configValueIn);
                    }
                }
                catch
                {
                }

                try
                {
                    iterator = navigator.Select(@"/configuration/hubRoleSettings/peerRouter");

                    if (iterator.MoveNext() == true)
                    {
                        configValueIn = iterator.Current.GetAttribute(@"numConnections", String.Empty);
                        if (configValueIn.Length != 0)
                            newConfigSettings.HubRoleSettings.PeerRouter.NumConnections = int.Parse(configValueIn);

                        configValueIn = iterator.Current.GetAttribute(@"port", String.Empty);
                        if (configValueIn.Length != 0)
                            newConfigSettings.HubRoleSettings.PeerRouter.Port = int.Parse(configValueIn);

                        configValueIn = iterator.Current.GetAttribute(@"bufferSize", String.Empty);
                        if (configValueIn.Length != 0)
                            newConfigSettings.HubRoleSettings.PeerRouter.BufferSize = int.Parse(configValueIn);

                        configValueIn = iterator.Current.GetAttribute(@"timeout", String.Empty);
                        if (configValueIn.Length != 0)
                            newConfigSettings.HubRoleSettings.PeerRouter.Timeout = int.Parse(configValueIn);
                    }
                }
                catch
                {
                }
            }

            internal static void LoadNodeRoleSettings(XPathNavigator navigator, ConfigSettings newConfigSettings)
            {
                string configValueIn;
                XPathNodeIterator iterator;

                try
                {
                    iterator = navigator.Select(@"/configuration/nodeRoleSettings/subscriptionManagement");

                    if (iterator.MoveNext() == true)
                    {
                        configValueIn = iterator.Current.GetAttribute(@"refreshIncrement", String.Empty);
                        if (configValueIn.Length != 0)
                            newConfigSettings.NodeRoleSettings.SubscriptionManagement.RefreshIncrement = Int32.Parse(configValueIn);

                        configValueIn = iterator.Current.GetAttribute(@"expirationIncrement", String.Empty);
                        if (configValueIn.Length != 0)
                            newConfigSettings.NodeRoleSettings.SubscriptionManagement.ExpirationIncrement = Int32.Parse(configValueIn);
                    }
                }
                catch
                {
                }

                try
                {
                    iterator = navigator.Select(@"/configuration/nodeRoleSettings/localPublish");

                    if (iterator.MoveNext() == true)
                    {
                        configValueIn = iterator.Current.GetAttribute(@"eventQueueName", String.Empty);
                        if (configValueIn.Length != 0)
                            newConfigSettings.NodeRoleSettings.LocalPublish.EventQueueName = configValueIn;

                        configValueIn = iterator.Current.GetAttribute(@"eventQueueSize", String.Empty);
                        if (configValueIn.Length != 0)
                            newConfigSettings.NodeRoleSettings.LocalPublish.EventQueueSize = UInt32.Parse(configValueIn);

                        configValueIn = iterator.Current.GetAttribute(@"averageEventSize", String.Empty);
                        if (configValueIn.Length != 0)
                            newConfigSettings.NodeRoleSettings.LocalPublish.AverageEventSize = Int32.Parse(configValueIn);
                    }
                }
                catch
                {
                }

                try
                {
                    iterator = navigator.Select(@"/configuration/nodeRoleSettings/outputCommunicationQueues");

                    if (iterator.MoveNext() == true)
                    {
                        configValueIn = iterator.Current.GetAttribute(@"maxQueueSize", String.Empty);
                        if (configValueIn.Length != 0)
                            newConfigSettings.NodeRoleSettings.OutputCommunicationQueues.MaxQueueSize = Int32.Parse(configValueIn);

                        configValueIn = iterator.Current.GetAttribute(@"maxTimeout", String.Empty);
                        if (configValueIn.Length != 0)
                            newConfigSettings.NodeRoleSettings.OutputCommunicationQueues.MaxTimeout = Int32.Parse(configValueIn);
                    }
                }
                catch
                {
                }

                try
                {
                    iterator = navigator.Select(@"/configuration/nodeRoleSettings/parentRouter");

                    if (iterator.MoveNext() == true)
                    {
                        configValueIn = iterator.Current.GetAttribute(@"numConnections", String.Empty);
                        if (configValueIn.Length != 0)
                            newConfigSettings.NodeRoleSettings.ParentRouter.NumConnections = int.Parse(configValueIn);

                        configValueIn = iterator.Current.GetAttribute(@"port", String.Empty);
                        if (configValueIn.Length != 0)
                            newConfigSettings.NodeRoleSettings.ParentRouter.Port = int.Parse(configValueIn);

                        configValueIn = iterator.Current.GetAttribute(@"bufferSize", String.Empty);
                        if (configValueIn.Length != 0)
                            newConfigSettings.NodeRoleSettings.ParentRouter.BufferSize = int.Parse(configValueIn);

                        configValueIn = iterator.Current.GetAttribute(@"timeout", String.Empty);
                        if (configValueIn.Length != 0)
                            newConfigSettings.NodeRoleSettings.ParentRouter.Timeout = int.Parse(configValueIn);
                    }
                }
                catch
                {
                }
            }

            internal static void LoadGroupSettings(XPathNavigator navigator, ConfigSettings newConfigSettings)
            {
                string configValueIn;
                XPathNodeIterator iterator;

                try
                {
                    iterator = navigator.Select(@"/configuration/groupSettings/group");

                    while (iterator.MoveNext() == true)
                    {
                        Group group = new Group();
                        XPathNodeIterator child;

                        configValueIn = iterator.Current.GetAttribute(@"name", String.Empty);
                        if (configValueIn.Length != 0)
                            group.Name = configValueIn;

                        configValueIn = iterator.Current.GetAttribute(@"useGroup", String.Empty);
                        if (configValueIn.Length != 0)
                            group.UseGroup = configValueIn;

                        child = iterator.Current.SelectChildren(@"hub", string.Empty);

                        while (child.MoveNext() == true)
                        {
                            Hub hub = new Hub();

                            configValueIn = child.Current.GetAttribute(@"name", String.Empty);
                            if (configValueIn.Length != 0)
                            {
                                try
                                {
                                    IPHostEntry hostEntry = Dns.GetHostEntry(configValueIn);

                                    if (string.IsNullOrEmpty(hostEntry.HostName) == true)
                                    {
                                        EventLog.WriteEntry("WspEventRouter", "Hub entry [" + configValueIn + "] cannot be resolved by DNS", EventLogEntryType.Error);
                                    }
                                    else
                                    {
                                        char[] splitChar = { '.' };

                                        string[] temp = hostEntry.HostName.ToLower().Split(splitChar, 2);

                                        hub.Name = temp[0];

                                        group.Hubs.Add(hub.Name, hub);
                                    }
                                }
                                catch (Exception e)
                                {
                                    EventLog.WriteEntry("WspEventRouter", "Hub entry [" + configValueIn + "] cannot be resolved by DNS.  Exception: " + e.Message, EventLogEntryType.Error);
                                }
                            }
                        }

                        newConfigSettings.GroupSettings.Groups.Add(group.Name, group);
                    }
                }
                catch
                {
                }
            }

            internal static void LoadLogSettings()
            {
                string configValueIn;
                Guid eventType;

                string configFile = AppDomain.CurrentDomain.SetupInformation.ApplicationBase +
                    AppDomain.CurrentDomain.FriendlyName + ".config";

                XPathDocument document = new XPathDocument(configFile);
                XPathNavigator navigator = document.CreateNavigator();
                XPathNodeIterator iterator;

                Persister.lastConfigFileTick = GetConfigFileTick();

                iterator = navigator.Select(@"/configuration/logSettings/log");

                while (iterator.MoveNext() == true)
                {
                    PersistEventInfo eventInfo;

                    configValueIn = iterator.Current.GetAttribute(@"eventType", String.Empty);

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
                    if (string.IsNullOrEmpty(configValueIn) == true)
                    {
                        eventInfo.LocalOnly = true;
                    }
                    else
                    {
                        eventInfo.LocalOnly = bool.Parse(configValueIn);
                    }

                    configValueIn = iterator.Current.GetAttribute(@"maxFileSize", String.Empty);
                    if (string.IsNullOrEmpty(configValueIn) == false)
                        eventInfo.MaxFileSize = long.Parse(configValueIn);

                    configValueIn = iterator.Current.GetAttribute(@"maxCopyInterval", String.Empty);
                    if (string.IsNullOrEmpty(configValueIn) == false)
                        eventInfo.CopyIntervalTicks = long.Parse(configValueIn) * 10000000;

                    configValueIn = iterator.Current.GetAttribute(@"createEmptyFiles", String.Empty);
                    if (string.IsNullOrEmpty(configValueIn) == false && string.Compare(configValueIn, "true", true) == 0)
                        eventInfo.CreateEmptyFiles = true;

                    configValueIn = iterator.Current.GetAttribute(@"fieldTerminator", String.Empty);
                    if (string.IsNullOrEmpty(configValueIn) == true)
                    {
                        eventInfo.FieldTerminator = ',';
                    }
                    else
                    {
                        eventInfo.FieldTerminator = ConvertDelimeter(configValueIn);
                    }

                    configValueIn = iterator.Current.GetAttribute(@"rowTerminator", String.Empty);
                    if (string.IsNullOrEmpty(configValueIn) == true)
                    {
                        eventInfo.RowTerminator = '\n';
                    }
                    else
                    {
                        eventInfo.RowTerminator = ConvertDelimeter(configValueIn);
                    }

                    configValueIn = iterator.Current.GetAttribute(@"keyValueSeparator", String.Empty);
                    if (string.IsNullOrEmpty(configValueIn) == true)
                    {
                        eventInfo.KeyValueSeparator = ':';
                    }
                    else
                    {
                        eventInfo.KeyValueSeparator = ConvertDelimeter(configValueIn);
                    }

                    configValueIn = iterator.Current.GetAttribute(@"beginObjectSeparator", String.Empty);
                    if (string.IsNullOrEmpty(configValueIn) == true)
                    {
                        eventInfo.BeginObjectSeparator = '{';
                    }
                    else
                    {
                        eventInfo.BeginObjectSeparator = ConvertDelimeter(configValueIn);
                    }

                    configValueIn = iterator.Current.GetAttribute(@"endObjectSeparator", String.Empty);
                    if (string.IsNullOrEmpty(configValueIn) == true)
                    {
                        eventInfo.EndObjectSeparator = '}';
                    }
                    else
                    {
                        eventInfo.EndObjectSeparator = ConvertDelimeter(configValueIn);
                    }

                    configValueIn = iterator.Current.GetAttribute(@"beginArraySeparator", String.Empty);
                    if (string.IsNullOrEmpty(configValueIn) == true)
                    {
                        eventInfo.BeginArraySeparator = '[';
                    }
                    else
                    {
                        eventInfo.BeginArraySeparator = ConvertDelimeter(configValueIn);
                    }

                    configValueIn = iterator.Current.GetAttribute(@"endArraySeparator", String.Empty);
                    if (string.IsNullOrEmpty(configValueIn) == true)
                    {
                        eventInfo.EndArraySeparator = ']';
                    }
                    else
                    {
                        eventInfo.EndArraySeparator = ConvertDelimeter(configValueIn);
                    }

                    configValueIn = iterator.Current.GetAttribute(@"stringCharacter", String.Empty);
                    if (string.IsNullOrEmpty(configValueIn) == true)
                    {
                        eventInfo.StringDelimiter = '"';
                    }
                    else
                    {
                        eventInfo.StringDelimiter = ConvertDelimeter(configValueIn);
                    }

                    configValueIn = iterator.Current.GetAttribute(@"escapeCharacter", String.Empty);
                    if (string.IsNullOrEmpty(configValueIn) == true)
                    {
                        eventInfo.EscapeCharacter = '\\';
                    }
                    else
                    {
                        eventInfo.EscapeCharacter = configValueIn[0];
                    }

                    configValueIn = iterator.Current.GetAttribute(@"tempFileDirectory", String.Empty);
                    if (string.IsNullOrEmpty(configValueIn) == true)
                    {
                        configValueIn = @"C:\temp\" + Guid.NewGuid().ToString() + @"\";
                    }

                    eventInfo.TempFileDirectory = configValueIn;

                    if (Directory.Exists(configValueIn) == false)
                        Directory.CreateDirectory(configValueIn);

                    configValueIn = iterator.Current.GetAttribute(@"copyToFileDirectory", String.Empty);
                    if (string.IsNullOrEmpty(configValueIn) == true)
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

            internal static void LoadLocalLogSettings()
            {
                string configValueIn;
                Guid eventType;

                string configFile = AppDomain.CurrentDomain.SetupInformation.ApplicationBase +
                    AppDomain.CurrentDomain.FriendlyName + ".local.config";

                if (File.Exists(configFile) == false)
                {
                    return;
                }

                XPathDocument document = new XPathDocument(configFile);
                XPathNavigator navigator = document.CreateNavigator();
                XPathNodeIterator iterator;

                Persister.lastLocalConfigFileTick = GetLocalConfigFileTick();

                iterator = navigator.Select(@"/configuration/logSettings/log");

                while (iterator.MoveNext() == true)
                {
                    PersistEventInfo eventInfo;

                    configValueIn = iterator.Current.GetAttribute(@"eventType", String.Empty);

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
                    if (string.IsNullOrEmpty(configValueIn) == true)
                    {
                        eventInfo.LocalOnly = true;
                    }
                    else
                    {
                        eventInfo.LocalOnly = bool.Parse(configValueIn);
                    }

                    configValueIn = iterator.Current.GetAttribute(@"maxFileSize", String.Empty);
                    if (string.IsNullOrEmpty(configValueIn) == false)
                        eventInfo.MaxFileSize = long.Parse(configValueIn);

                    configValueIn = iterator.Current.GetAttribute(@"maxCopyInterval", String.Empty);
                    if (string.IsNullOrEmpty(configValueIn) == false)
                        eventInfo.CopyIntervalTicks = long.Parse(configValueIn) * 10000000;

                    configValueIn = iterator.Current.GetAttribute(@"createEmptyFiles", String.Empty);
                    if (string.IsNullOrEmpty(configValueIn) == false && string.Compare(configValueIn, "true", true) == 0)
                        eventInfo.CreateEmptyFiles = true;

                    configValueIn = iterator.Current.GetAttribute(@"fieldTerminator", String.Empty);
                    if (string.IsNullOrEmpty(configValueIn) == true)
                    {
                        eventInfo.FieldTerminator = ',';
                    }
                    else
                    {
                        eventInfo.FieldTerminator = ConvertDelimeter(configValueIn);
                    }

                    configValueIn = iterator.Current.GetAttribute(@"rowTerminator", String.Empty);
                    if (string.IsNullOrEmpty(configValueIn) == true)
                    {
                        eventInfo.RowTerminator = '\n';
                    }
                    else
                    {
                        eventInfo.RowTerminator = ConvertDelimeter(configValueIn);
                    }

                    configValueIn = iterator.Current.GetAttribute(@"keyValueSeparator", String.Empty);
                    if (string.IsNullOrEmpty(configValueIn) == true)
                    {
                        eventInfo.KeyValueSeparator = ':';
                    }
                    else
                    {
                        eventInfo.KeyValueSeparator = ConvertDelimeter(configValueIn);
                    }

                    configValueIn = iterator.Current.GetAttribute(@"beginObjectSeparator", String.Empty);
                    if (string.IsNullOrEmpty(configValueIn) == true)
                    {
                        eventInfo.BeginObjectSeparator = '{';
                    }
                    else
                    {
                        eventInfo.BeginObjectSeparator = ConvertDelimeter(configValueIn);
                    }

                    configValueIn = iterator.Current.GetAttribute(@"endObjectSeparator", String.Empty);
                    if (string.IsNullOrEmpty(configValueIn) == true)
                    {
                        eventInfo.EndObjectSeparator = '}';
                    }
                    else
                    {
                        eventInfo.EndObjectSeparator = ConvertDelimeter(configValueIn);
                    }

                    configValueIn = iterator.Current.GetAttribute(@"beginArraySeparator", String.Empty);
                    if (string.IsNullOrEmpty(configValueIn) == true)
                    {
                        eventInfo.BeginArraySeparator = '[';
                    }
                    else
                    {
                        eventInfo.BeginArraySeparator = ConvertDelimeter(configValueIn);
                    }

                    configValueIn = iterator.Current.GetAttribute(@"endArraySeparator", String.Empty);
                    if (string.IsNullOrEmpty(configValueIn) == true)
                    {
                        eventInfo.EndArraySeparator = ']';
                    }
                    else
                    {
                        eventInfo.EndArraySeparator = ConvertDelimeter(configValueIn);
                    }

                    configValueIn = iterator.Current.GetAttribute(@"stringCharacter", String.Empty);
                    if (string.IsNullOrEmpty(configValueIn) == true)
                    {
                        eventInfo.StringDelimiter = '"';
                    }
                    else
                    {
                        eventInfo.StringDelimiter = ConvertDelimeter(configValueIn);
                    }

                    configValueIn = iterator.Current.GetAttribute(@"escapeCharacter", String.Empty);
                    if (string.IsNullOrEmpty(configValueIn) == true)
                    {
                        eventInfo.EscapeCharacter = '\\';
                    }
                    else
                    {
                        eventInfo.EscapeCharacter = configValueIn[0];
                    }

                    configValueIn = iterator.Current.GetAttribute(@"tempFileDirectory", String.Empty);
                    if (string.IsNullOrEmpty(configValueIn) == true)
                    {
                        configValueIn = @"C:\temp\" + Guid.NewGuid().ToString() + @"\";
                    }

                    eventInfo.TempFileDirectory = configValueIn;

                    if (Directory.Exists(configValueIn) == false)
                        Directory.CreateDirectory(configValueIn);

                    configValueIn = iterator.Current.GetAttribute(@"copyToFileDirectory", String.Empty);
                    if (string.IsNullOrEmpty(configValueIn) == true)
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

            internal static void SaveNewConfigFile(ConfigSettings newConfigSettings, string originConfig)
            {
                if (string.IsNullOrEmpty(originConfig) == true)
                {
                    EventLog.WriteEntry("WspEventRouter", "Config file received is empty", EventLogEntryType.Error);

                    throw new Exception("Config file received is empty");
                }

                XmlDocument doc = new XmlDocument();
                doc.PreserveWhitespace = true;
                doc.LoadXml(originConfig);

                XmlNode node = doc.SelectSingleNode(@"descendant::configuration");
                XmlElement root = node["eventRouterSettings"];

                if (root.HasAttribute("mgmtGuid") == true)
                {
                    String mgmtGuidValue = root.GetAttribute("mgmtGuid");

                    if (string.IsNullOrEmpty(mgmtGuidValue) == true)
                    {
                        EventLog.WriteEntry("WspEventRouter", "Config data from Origin router does not contain a mgmtGuid value.", EventLogEntryType.Error);

                        throw new Exception("Error in origin config file, no mgmtGuid value found");
                    }
                }
                else
                {
                    EventLog.WriteEntry("WspEventRouter", "Config data from Origin router does not contain a mgmtGuid value.", EventLogEntryType.Error);

                    throw new Exception("Error in origin config file, not mgmtGuid value found");
                }

                root.SetAttribute("role", newConfigSettings.EventRouterSettings.Role);
                root.SetAttribute("group", newConfigSettings.EventRouterSettings.Group);
                root.SetAttribute("publish", newConfigSettings.EventRouterSettings.Publish.ToString());

                doc.Save(AppDomain.CurrentDomain.SetupInformation.ApplicationBase + AppDomain.CurrentDomain.FriendlyName + ".config");
            }

            internal static string GetOriginConfig(string bootstrapUrl)
            {
                HttpWebRequest request;
                HttpWebResponse response;
                Stream receiveStream;
                StreamReader readStream;
                string responseValue;

                if (bootstrapUrl == string.Empty)
                {
                    return string.Empty;
                }

                request = (HttpWebRequest)WebRequest.Create(bootstrapUrl);

                try
                {
                    response = (HttpWebResponse)request.GetResponse();
                }
                catch
                {
                    return string.Empty;
                }

                if (response.ContentLength == 0)
                {
                    return string.Empty;
                }

                receiveStream = response.GetResponseStream();

                readStream = new StreamReader(receiveStream, Encoding.UTF8);

                responseValue = readStream.ReadToEnd();

                response.Close();
                readStream.Close();

                return responseValue;
            }

            internal static long GetConfigFileTick()
            {
                string configFile = AppDomain.CurrentDomain.SetupInformation.ApplicationBase +
                    AppDomain.CurrentDomain.FriendlyName + ".config";

                return (new FileInfo(configFile)).LastWriteTimeUtc.Ticks;
            }

            internal static long GetLocalConfigFileTick()
            {
                string configFile = AppDomain.CurrentDomain.SetupInformation.ApplicationBase +
                    AppDomain.CurrentDomain.FriendlyName + ".local.config";

                if (File.Exists(configFile) == false)
                {
                    return 0;
                }

                return (new FileInfo(configFile)).LastWriteTimeUtc.Ticks;
            }

            internal static char ConvertDelimeter(string delimeterIn)
            {
                char delimeterOut = ',';

                if (delimeterIn.Length == 1)
                {
                    return delimeterIn[0];
                }

                switch (delimeterIn.Substring(0, 2))
                {
                    case @"\a":
                        delimeterOut = '\a';
                        break;

                    case @"\b":
                        delimeterOut = '\b';
                        break;

                    case @"\f":
                        delimeterOut = '\f';
                        break;

                    case @"\n":
                        delimeterOut = '\n';
                        break;

                    case @"\r":
                        delimeterOut = '\r';
                        break;

                    case @"\t":
                        delimeterOut = '\t';
                        break;

                    case @"\v":
                        delimeterOut = '\v';
                        break;

                    case @"\u":
                        if (delimeterIn.Length == 6)
                        {
                            int x = 0;

                            for (int i = 2; i < delimeterIn.Length; i++)
                            {
                                x = x << 4;

                                x = x + Convert.ToInt32(delimeterIn.Substring(i, 1));
                            }

                            delimeterOut = Convert.ToChar(x);
                        }
                        break;

                    default:
                        break;
                }

                return delimeterOut;
            }
        }
    }
}
