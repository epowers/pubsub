using System;
using System.Runtime.InteropServices;
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

namespace Microsoft.WebSolutionsPlatform.Router
{
    public partial class Router : ServiceBase
    {
        internal class SocketInfo
        {
            private List<Socket> sockets;
            /// <summary>
            /// List of inbound sockets to this node
            /// </summary>
            public List<Socket> Sockets
            {
                get
                {
                    return sockets;
                }
            }

            private string routerName;
            /// <summary>
            /// Name of the event router
            /// </summary>
            public string RouterName
            {
                get
                {
                    return routerName;
                }

                set
                {
                    routerName = value;
                }
            }

            private byte[] routerNameEncodedPriv = null;
            internal byte[] routerNameEncoded
            {
                get
                {
                    if (routerNameEncodedPriv == null && string.IsNullOrEmpty(RouterName) == false)
                    {
                        UTF8Encoding uniEncoding = new UTF8Encoding();
                        routerNameEncodedPriv = uniEncoding.GetBytes(RouterName);
                    }

                    return routerNameEncodedPriv;
                }
            }

            private string group;
            /// <summary>
            /// Group which the event router belongs to
            /// </summary>
            public string Group
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

            private bool hub;
            /// <summary>
            /// True if this router is a hub
            /// </summary>
            public bool Hub
            {
                get
                {
                    return hub;
                }

                set
                {
                    hub = value;
                }
            }

            private bool useToSend;
            /// <summary>
            /// True if router should send events via these sockets
            /// </summary>
            public bool UseToSend
            {
                get
                {
                    return useToSend;
                }

                set
                {
                    useToSend = value;
                }
            }

            /// <summary>
            /// Base constructor to create a new SocketInfo
            /// </summary>
            public SocketInfo()
            {
                routerName = string.Empty;
                useToSend = true;
                sockets = new List<Socket>();
            }

            public static SocketInfo Clone(SocketInfo socketInfoIn)
            {
                SocketInfo socketInfoOut = new SocketInfo();

                socketInfoOut.Group = socketInfoIn.Group;
                socketInfoOut.Hub = socketInfoIn.Hub;
                socketInfoOut.UseToSend = socketInfoIn.useToSend;
                socketInfoOut.RouterName = socketInfoIn.RouterName;

                for (int i = 0; i < socketInfoIn.Sockets.Count; i++)
                {
                    socketInfoOut.Sockets.Add(socketInfoIn.Sockets[i]);
                }

                return socketInfoOut;
            }
        }

        internal class Communicator : ServiceThread
        {
            internal static Thread serverListenThread;
            internal static bool abortParent = false;
            internal static string parentRouterName = string.Empty;
            internal static Thread distributeThread;

            internal static Dictionary<string, RefCount> serverSendThreads = new Dictionary<string, RefCount>();
            internal static object sendThreadQueueLock = new object();

            internal static object socketQueuesLock = new object();

            internal static Dictionary<string, SocketInfo> commSockets = new Dictionary<string, SocketInfo>(StringComparer.CurrentCultureIgnoreCase);
            internal static Dictionary<string, SocketInfo> peerSockets = new Dictionary<string, SocketInfo>(StringComparer.CurrentCultureIgnoreCase);

            internal static Dictionary<string, SynchronizationQueue<QueueElement>> socketQueues =
                new Dictionary<string, SynchronizationQueue<QueueElement>>(StringComparer.CurrentCultureIgnoreCase);

            internal static Dictionary<string, SocketInfo> deadSocketQueues = new Dictionary<string, SocketInfo>(StringComparer.CurrentCultureIgnoreCase);

            internal static SynchronizationQueue<SendState> sendStateObjects = new SynchronizationQueue<SendState>();

            public Communicator()
            {
            }

            public override void Start()
            {
                while (true)
                {
                    try
                    {
                        if (distributeThread == null || distributeThread.ThreadState == System.Threading.ThreadState.Stopped)
                        {
                            if (distributeThread != null && distributeThread.ThreadState == System.Threading.ThreadState.Stopped)
                            {
                                distributeThread.Abort();
                            }

                            distributeThread = new Thread(new ThreadStart(new DistributeHandler().Start));

                            distributeThread.Start();
                        }

                        if (hubRole == true)
                        {
                            ManageHub();
                        }
                        else
                        {
                            ManageNode();
                        }

                        CleanupQueues();

                        if (hubRole == true)
                        {
                            Thread.Sleep(configSettings.HubRoleSettings.PeerRouter.Timeout);
                        }
                        else
                        {
                            Thread.Sleep(configSettings.NodeRoleSettings.ParentRouter.Timeout);
                        }
                    }

                    catch (ThreadAbortException)
                    {
                        // Another thread has signalled that this worker
                        // thread must terminate.  Typically, this occurs when
                        // the main service thread receives a service stop 
                        // command.

                        if (distributeThread != null)
                        {
                            distributeThread.Abort();
                        }
                    }

                    catch (Exception e)
                    {
                        EventLog.WriteEntry("WspEventRouter", e.ToString(), EventLogEntryType.Warning);
                    }
                }
            }

            internal void ManageHub()
            {
                Dictionary<string, Hub> hubs;
                Dictionary<string, string> peers;
                string groupName;

                try
                {
                    if (configSettings.HubRoleSettings.ThisRouter.Port != 0)
                    {
                        if (serverListenThread == null || serverListenThread.ThreadState == System.Threading.ThreadState.Stopped)
                        {
                            if (serverListenThread != null &&
                                serverListenThread.ThreadState == System.Threading.ThreadState.Stopped)
                            {
                                serverListenThread.Abort();
                            }

                            serverListenThread = new Thread(new ThreadStart(new ListenHandler().Start));

                            serverListenThread.Start();
                        }
                    }

                    hubs = GetHubList(out groupName);

                    ConnectToGroup(hubs, groupName);

                    peers = GetPeerList(groupName);

                    ConnectToPeers(peers, groupName);
                }
                catch (ThreadAbortException e)
                {
                    throw e;
                }
                catch
                {
                }
            }

            internal Dictionary<string, Hub> GetHubList(out string groupName)
            {
                Group currGroup = null;
                groupName = configSettings.EventRouterSettings.Group;

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

            internal Dictionary<string, string> GetPeerList(string groupName)
            {
                Dictionary<string, string> peers = new Dictionary<string, string>();

                foreach (Group group in configSettings.GroupSettings.Groups.Values)
                {
                    if (string.IsNullOrEmpty(group.UseGroup) == false || string.Compare(group.Name, groupName, true) == 0)
                    {
                        continue;
                    }

                    if (peerSockets.ContainsKey(group.Name) == false)
                    {
                        Random randObj = new Random();
                        int item = randObj.Next(group.Hubs.Count);
                        int i = 0;

                        foreach (string name in group.Hubs.Keys)
                        {
                            if (i == item)
                            {
                                peers[group.Name] = name;
                                break;
                            }

                            i++;
                        }
                    }
                }

                return peers;
            }

            internal void ConnectToGroup(Dictionary<string, Hub> hubs, string groupName)
            {
                SocketInfo socketInfo;
                IPAddress[] ipLocal = Dns.GetHostAddresses(string.Empty);

                foreach (Hub hub in hubs.Values)
                {
                    if (string.Compare(hub.Name, LocalRouterName, true) == 0)
                    {
                        continue;
                    }

                    bool localAddress = false;

                    IPAddress[] ipHub = Dns.GetHostAddresses(hub.Name);

                    for (int i = 0; i < ipLocal.Length; i++)
                    {
                        for (int j = 0; j < ipHub.Length; j++)
                        {
                            if (IPAddress.Equals(ipLocal[i], ipHub[j]) == true)
                            {
                                localAddress = true;

                                break;
                            }
                        }

                        if (localAddress == true)
                        {
                            break;
                        }
                    }

                    if (localAddress == true)
                    {
                        continue;
                    }

                    if (commSockets.TryGetValue(hub.Name, out socketInfo) == false)
                    {
                        socketInfo = new SocketInfo();
                        socketInfo.Group = groupName;
                        socketInfo.Hub = true;
                        socketInfo.RouterName = hub.Name;
                    }

                    int numConnections = configSettings.HubRoleSettings.PeerRouter.NumConnections - socketInfo.Sockets.Count;

                    if (numConnections > 0)
                    {
                        CreateConnections(hub.Name, groupName, configSettings.HubRoleSettings.PeerRouter.Port, configSettings.HubRoleSettings.PeerRouter.Timeout,
                            configSettings.HubRoleSettings.PeerRouter.NumConnections, socketInfo);
                    }
                }
            }

            internal void ConnectToPeers(Dictionary<string, string> peers, string groupName)
            {
                SocketInfo socketInfo;

                foreach (string peerGroupName in peers.Keys)
                {
                    if (commSockets.TryGetValue(peers[peerGroupName], out socketInfo) == true)
                    {
                        if (socketInfo.UseToSend == false)
                        {
                            socketInfo.UseToSend = true;
                            peerSockets[peers[peerGroupName]] = socketInfo;
                        }
                    }
                    else
                    {
                        socketInfo = new SocketInfo();
                        socketInfo.Group = peerGroupName;
                        socketInfo.Hub = true;
                        socketInfo.RouterName = peers[peerGroupName];
                        socketInfo.UseToSend = true;
                    }

                    int numConnections = configSettings.HubRoleSettings.PeerRouter.NumConnections - socketInfo.Sockets.Count;

                    if (numConnections > 0)
                    {
                        CreateConnections(peers[peerGroupName], groupName, configSettings.HubRoleSettings.PeerRouter.Port, configSettings.HubRoleSettings.PeerRouter.Timeout,
                            configSettings.HubRoleSettings.PeerRouter.NumConnections, socketInfo);
                    }
                }
            }

            internal void ManageNode()
            {
                int i;

                try
                {
                    if (parentRoute != null && commSockets[parentRoute.RouterName].Sockets.Count == 0)
                    {
                        if (commSockets.ContainsKey(parentRoute.RouterName) == true)
                        {
                            commSockets.Remove(parentRoute.RouterName);
                        }

                        parentRoute = null;
                    }

                    if (parentRoute == null)
                    {
                        SocketInfo socketInfo = new SocketInfo();

                        parentRoute = SetParentRoute(socketInfo);

                        parentRouterName = parentRoute.RouterName;

                        commSockets[parentRouterName] = socketInfo;
                    }

                    lock (socketQueuesLock)
                    {
                        for (i = 0; i < commSockets[parentRouterName].Sockets.Count; i++)
                        {
                            Socket parentSocket = commSockets[parentRouterName].Sockets[i];

                            if (parentSocket == null || parentSocket.Connected == false)
                            {
                                commSockets[parentRouterName].Sockets.RemoveAt(i);
                                i--;
                            }
                        }
                    }

                    if (commSockets.Count == 0)
                    {
                        string oldParentName = parentRouterName;

                        parentRoute = SetParentRoute(commSockets[parentRouterName]);

                        parentRouterName = parentRoute.RouterName;

                        if (string.Compare(oldParentName, parentRouterName, true) != 0)
                        {
                            if (commSockets.ContainsKey(oldParentName) == true)
                            {
                                commSockets[parentRouterName] = commSockets[oldParentName];
                                commSockets.Remove(oldParentName);
                            }
                        }
                    }

                    int startingSocketCount = commSockets[parentRouterName].Sockets.Count;

                    int numConnections = parentRoute.NumConnections - commSockets[parentRouterName].Sockets.Count;

                    if (numConnections > 0)
                    {
                        CreateConnections(parentRoute.RouterName, commSockets[parentRouterName].Group, parentRoute.Port, parentRoute.Timeout,
                            numConnections, commSockets[parentRouterName]);
                    }

                    if (startingSocketCount == 0 && commSockets[parentRouterName].Sockets.Count > 0)
                    {
                        SubscriptionMgr.ResendSubscriptions(parentRoute.RouterName);
                    }
                }
                catch (ThreadAbortException e)
                {
                    throw e;
                }
                catch
                {
                }
            }

            private void CreateConnections(string routerName, string currGroupName, int port, int timeout, int numConnections, SocketInfo socketInfo)
            {
                PerformanceCounter threadSocketCounter;
                SynchronizationQueue<QueueElement> socketQueue;

                if (string.IsNullOrEmpty(routerName) == true)
                {
                    return;
                }

                for (int i = 0; i < numConnections; i++)
                {
                    Socket socket = OpenConnection(routerName, port, timeout);

                    if (socket != null)
                    {
                        lock (socketQueuesLock)
                        {
                            if (hubRole == true)
                            {
                                if (commSockets.ContainsKey(routerName) == false)
                                {
                                    socketInfo.Sockets.Add(socket);
                                    commSockets[routerName] = socketInfo;

                                    if (socketInfo.UseToSend == true)
                                    {
                                        peerSockets[routerName] = socketInfo;
                                    }
                                }
                                else
                                {
                                    commSockets[routerName].Sockets.Add(socket);
                                }
                            }
                            else
                            {
                                commSockets[parentRouterName].Sockets.Add(socket);
                            }

                            if (socketQueues.TryGetValue(routerName, out socketQueue) == false)
                            {
                                threadSocketCounter = new PerformanceCounter();
                                threadSocketCounter.InstanceLifetime = PerformanceCounterInstanceLifetime.Process;
                                threadSocketCounter.CategoryName = communicationCategoryName;
                                threadSocketCounter.CounterName = forwarderQueueSizeName;
                                threadSocketCounter.InstanceName = routerName;
                                threadSocketCounter.ReadOnly = false;

                                socketQueue = new SynchronizationQueue<QueueElement>(threadSocketCounter);

                                socketQueues[routerName] = socketQueue;

                                socketQueue.InUse = true;
                            }
                            else
                            {
                                if (socketQueue.InUse != true)
                                {
                                    socketQueue.InUse = true;

                                    if (deadSocketQueues.ContainsKey(routerName) == true)
                                    {
                                        deadSocketQueues.Remove(routerName);
                                    }
                                }
                            }
                        }

                        Thread inConnection =
                            new Thread(new ThreadStart(new CommunicationHandler(socket, socketInfo, socketQueue).ConnectionInStart));

                        inConnection.Start();

                        if (socketInfo.UseToSend == true || string.Compare(socketInfo.Group, currGroupName, true) == 0)
                        {
                            Thread outConnection =
                                new Thread(new ThreadStart(new CommunicationHandler(socket, socketInfo, socketQueue).ConnectionOutStart));

                            outConnection.Start();
                        }
                    }
                }

                return;
            }

            internal void CleanupQueues()
            {
                int i;
                long currentTickTimeout;
                PerformanceCounter socketQueueCounter;
                List<string> removeRouters = new List<string>();
                List<KeyValuePair<string, Socket>> removeSockets = new List<KeyValuePair<string, Socket>>();

                lock (socketQueuesLock)
                {
                    foreach (string routerName in commSockets.Keys)
                    {
                        foreach (Socket socket in commSockets[routerName].Sockets)
                        {
                            if (socket.Connected == false)
                            {
                                removeSockets.Add(new KeyValuePair<string, Socket>(routerName, socket));
                                removeRouters.Add(routerName);
                            }
                        }
                    }

                    if (removeSockets.Count > 0)
                    {
                        for (i = 0; i < removeSockets.Count; i++)
                        {
                            commSockets[removeSockets[i].Key].Sockets.Remove(removeSockets[i].Value);
                        }

                        removeSockets.Clear();
                    }

                    if (removeRouters.Count > 0)
                    {
                        for (i = 0; i < removeRouters.Count; i++)
                        {
                            if (commSockets.ContainsKey(removeRouters[i]) == true)
                            {
                                if (commSockets[removeRouters[i]].Sockets.Count == 0)
                                {
                                    socketQueues[removeRouters[i]].InUse = false;
                                    socketQueues[removeRouters[i]].LastUsedTick = DateTime.Now.Ticks;

                                    deadSocketQueues[removeRouters[i]] = commSockets[removeRouters[i]];

                                    if (commSockets[removeRouters[i]].UseToSend == true)
                                    {
                                        commSockets[removeRouters[i]].UseToSend = false;
                                        peerSockets.Remove(removeRouters[i]);
                                    }

                                    commSockets.Remove(removeRouters[i]);
                                }
                            }
                        }

                        removeRouters.Clear();
                    }

                    if (deadSocketQueues.Count > 0)
                    {
                        currentTickTimeout = DateTime.Now.Ticks - (((long)outputCommunicationQueues.MaxTimeout) * 10000000);

                        removeRouters.Clear();

                        foreach (string threadQueueName in deadSocketQueues.Keys)
                        {
                            try
                            {
                                if (socketQueues[threadQueueName].LastUsedTick < currentTickTimeout ||
                                    socketQueues[threadQueueName].Size > outputCommunicationQueues.MaxQueueSize)
                                {
                                    removeRouters.Add(threadQueueName);
                                }
                            }
                            catch
                            {
                                removeRouters.Add(threadQueueName);
                            }
                        }

                        if (removeRouters.Count > 0)
                        {
                            for (i = 0; i < removeRouters.Count; i++)
                            {
                                try
                                {
                                    socketQueueCounter = new PerformanceCounter();
                                    socketQueueCounter.InstanceLifetime = PerformanceCounterInstanceLifetime.Process;
                                    socketQueueCounter.CategoryName = communicationCategoryName;
                                    socketQueueCounter.CounterName = forwarderQueueSizeName;
                                    socketQueueCounter.InstanceName = removeRouters[i];
                                    socketQueueCounter.ReadOnly = false;

                                    socketQueueCounter.RemoveInstance();

                                    socketQueues[removeRouters[i]].Clear();
                                    socketQueues.Remove(removeRouters[i]);
                                }
                                finally
                                {
                                    deadSocketQueues.Remove(removeRouters[i]);
                                }
                            }

                            removeRouters.Clear();
                        }
                    }
                }
            }

            internal Route SetParentRoute(SocketInfo socketInfo)
            {
                string parentName = string.Empty;
                string groupName = configSettings.EventRouterSettings.Group;
                Group group;

                while (true)
                {
                    if (configSettings.GroupSettings.Groups.TryGetValue(groupName, out group) == true)
                    {
                        if (string.IsNullOrEmpty(group.UseGroup) == false)
                        {
                            if (string.Compare(groupName, group.UseGroup, true) != 0)
                            {
                                groupName = group.UseGroup;
                                continue;
                            }
                        }
                    }
                    else
                    {
                        if (string.Compare(groupName, "default", true) == 0)
                        {
                            return new Route(string.Empty,
                                configSettings.NodeRoleSettings.ParentRouter.NumConnections,
                                configSettings.NodeRoleSettings.ParentRouter.Port,
                                configSettings.NodeRoleSettings.ParentRouter.BufferSize,
                                configSettings.NodeRoleSettings.ParentRouter.Timeout);
                        }
                        else
                        {
                            groupName = "default";
                            continue;
                        }
                    }

                    if (group.Hubs.Count > 0)
                    {
                        break;
                    }
                    else
                    {
                        if (string.Compare(groupName, "default", true) == 0)
                        {
                            return new Route(string.Empty,
                                configSettings.NodeRoleSettings.ParentRouter.NumConnections,
                                configSettings.NodeRoleSettings.ParentRouter.Port,
                                configSettings.NodeRoleSettings.ParentRouter.BufferSize,
                                configSettings.NodeRoleSettings.ParentRouter.Timeout);
                        }
                        else
                        {
                            Thread.Sleep(10000);

                            groupName = "default";

                            continue;
                        }
                    }
                }

                Random randObj = new Random();
                int item = randObj.Next(group.Hubs.Count);
                int i = 0;

                foreach (string name in group.Hubs.Keys)
                {
                    if (i == item)
                    {
                        parentName = name;
                        break;
                    }

                    i++;
                }

                socketInfo.RouterName = parentName;
                socketInfo.Hub = true;
                socketInfo.UseToSend = true;
                socketInfo.Group = groupName;

                return new Route(parentName,
                    configSettings.NodeRoleSettings.ParentRouter.NumConnections,
                    configSettings.NodeRoleSettings.ParentRouter.Port,
                    configSettings.NodeRoleSettings.ParentRouter.BufferSize,
                    configSettings.NodeRoleSettings.ParentRouter.Timeout);
            }

            internal Socket OpenConnection(string routerName, int port, int timeout)
            {
                Socket socket = null;
                SocketError socketError;
                byte[] preamble;
                byte[] inResponse = new byte[1];

                try
                {
                    socket = ConnectSocket(routerName, port);

                    if (socket == null)
                    {
                        return null;
                    }

                    socket.NoDelay = true;
                    socket.ReceiveTimeout = timeout;
                    socket.SendTimeout = timeout;

                    Int32 preambleLength = 0;
                    preambleLength += sizeof(Int32);
                    preambleLength += 1;
                    preambleLength += versionEncoded.Length + sizeof(Int32);
                    preambleLength += configSettings.EventRouterSettings.GroupEncoded.Length + sizeof(Int32);
                    preambleLength += routerNameEncoded.Length + sizeof(Int32);

                    preamble = new byte[preambleLength];

                    int location = 0;

                    Buffer.BlockCopy(BitConverter.GetBytes((Int32)preambleLength), 0, preamble, location, sizeof(Int32));
                    location += sizeof(Int32);

                    Buffer.BlockCopy(BitConverter.GetBytes(hubRole), 0, preamble, location, 1);
                    location += 1;

                    Buffer.BlockCopy(BitConverter.GetBytes((Int32)versionEncoded.Length), 0, preamble, location, sizeof(Int32));
                    location += sizeof(Int32);

                    Buffer.BlockCopy(versionEncoded, 0, preamble, location, versionEncoded.Length);
                    location += versionEncoded.Length;

                    Buffer.BlockCopy(BitConverter.GetBytes((Int32)configSettings.EventRouterSettings.GroupEncoded.Length), 0, preamble, location, sizeof(Int32));
                    location += sizeof(Int32);

                    Buffer.BlockCopy(configSettings.EventRouterSettings.GroupEncoded, 0, preamble, location, configSettings.EventRouterSettings.GroupEncoded.Length);
                    location += configSettings.EventRouterSettings.GroupEncoded.Length;

                    Buffer.BlockCopy(BitConverter.GetBytes((Int32)routerNameEncoded.Length), 0, preamble, location, sizeof(Int32));
                    location += sizeof(Int32);

                    Buffer.BlockCopy(routerNameEncoded, 0, preamble, location, routerNameEncoded.Length);

                    socket.Send(preamble, 0, preambleLength, SocketFlags.None, out socketError);

                    if (socketError != SocketError.Success)
                    {
                        CloseSocket(socket, routerName);
                        socket = null;

                        return null;
                    }

                    socket.Receive(inResponse, 0, inResponse.Length, SocketFlags.None, out socketError);

                    if (socketError != SocketError.Success || inResponse[0] != 1)
                    {
                        CloseSocket(socket, routerName);
                        socket = null;

                        return null;
                    }
                }
                catch (Exception e)
                {
                    EventLog.WriteEntry("WspEventRouter", e.ToString(), EventLogEntryType.Warning);
                }

                return socket;
            }

            private static Socket ConnectSocket(string server, int port)
            {
                Socket socket;
                IPHostEntry hostEntry = null;
                IPAddress addressIn = null;

                if (IPAddress.TryParse(server, out addressIn) == true)
                {
                    try
                    {
                        IPEndPoint ipe = new IPEndPoint(addressIn, port);

                        socket = new Socket(ipe.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

                        socket.Connect(ipe);

                        if (socket.Connected)
                        {
                            return socket;
                        }
                    }
                    catch
                    {
                        // Intentionally left empty, just loop to next.
                    }
                }
                else
                {
                    // Get host related information.
                    hostEntry = Dns.GetHostEntry(server);

                    // Loop through the AddressList to obtain the supported AddressFamily. This is to avoid
                    // an exception that occurs when the host IP Address is not compatible with the address family
                    // (typical in the IPv6 case).
                    foreach (IPAddress address in hostEntry.AddressList)
                    {
                        // Only use IPv4
                        if (address.AddressFamily == AddressFamily.InterNetworkV6)
                        {
                            continue;
                        }

                        try
                        {
                            IPEndPoint ipe = new IPEndPoint(address, port);

                            socket = new Socket(ipe.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

                            socket.Connect(ipe);

                            if (socket.Connected)
                            {
                                return socket;
                            }
                        }
                        catch
                        {
                            // Intentionally left empty, just loop to next.
                        }
                    }
                }

                return null;
            }

            internal static void CloseSocket(Socket socket, string clientRouterName)
            {
                if (socket != null)
                {
                    try
                    {
                        socket.Shutdown(SocketShutdown.Both);
                        socket.Close();
                    }
                    catch (Exception e)
                    {
                        EventLog.WriteEntry("WspEventRouter", "Router:" + clientRouterName + "  " + e.ToString(),
                            EventLogEntryType.Warning);
                    }
                }
            }
        }

        internal class ReceiveState
        {
            public Socket socket;
            public SocketError socketError;
            public SynchronizationQueue<QueueElement> socketQueue;
            public byte[] buffer;
            public ArrayList buffers;
            public int totalBytesRead;
            public ManualResetEvent receiveDone;
            public int currentReceiveLength;
            public byte[] currentReceiveLengthBytes;
            public int currentReceiveLengthBytesRead;
            public int currentProcessedLength;
            public SocketInfo socketInfo;

            internal ReceiveState()
            {
                socketError = SocketError.TryAgain;
                buffer = new byte[localPublish.AverageEventSize];
                buffers = new ArrayList(10);
                receiveDone = new ManualResetEvent(false);
                currentReceiveLengthBytes = new byte[4];
            }

            internal void Reset()
            {
                socketError = SocketError.TryAgain;
                totalBytesRead = 0;
                currentReceiveLength = 0;
                currentReceiveLengthBytesRead = 0;
                currentProcessedLength = 0;

                buffers.Clear();
                receiveDone.Reset();
            }
        }

        internal class RefCount
        {
            internal int Count = 0;

            internal RefCount()
            {
            }

            internal static void Decrement(string routerName)
            {
                RefCount refCount;

                if (Communicator.serverSendThreads.TryGetValue(routerName, out refCount) == true)
                {
                    Interlocked.Decrement(ref refCount.Count);
                }
            }
        }

        internal class SendState
        {
            private static QueueElement defaultElement = default(QueueElement);
            public SocketInfo socketInfo;
            public SynchronizationQueue<QueueElement> socketQueue;
            public QueueElement element;
            public Socket socket;

            internal SendState()
            {
                Reset();
            }

            internal static SendState Create(SocketInfo socketInfoIn)
            {
                SendState state = Communicator.sendStateObjects.Dequeue(10);

                if (state == null)
                {
                    state = new SendState();
                }

                if (Communicator.socketQueues.TryGetValue(socketInfoIn.RouterName, out state.socketQueue) == false)
                {
                    return null;
                }

                if (Communicator.commSockets.TryGetValue(socketInfoIn.RouterName, out state.socketInfo) == false)
                {
                    return null;
                }

                state.socketInfo = socketInfoIn;

                return state;
            }

            internal static SendState Create(string routerName)
            {
                SendState state = Communicator.sendStateObjects.Dequeue(10);

                if (state == null)
                {
                    state = new SendState();
                }

                if (Communicator.socketQueues.TryGetValue(routerName, out state.socketQueue) == false)
                {
                    Recycle(state);
                    return null;
                }

                if (Communicator.commSockets.TryGetValue(routerName, out state.socketInfo) == false)
                {
                    state.socketInfo = new SocketInfo();
                    state.socketInfo.RouterName = routerName;
                }

                return state;
            }

            internal static SendState Clone(SendState stateIn)
            {
                SendState stateOut = Communicator.sendStateObjects.Dequeue(10);

                if (stateOut == null)
                {
                    stateOut = new SendState();
                }

                stateOut.socketInfo = stateIn.socketInfo;
                stateOut.socketQueue = stateIn.socketQueue;
                stateOut.socketInfo = stateIn.socketInfo;

                return stateOut;
            }

            internal static void Recycle(SendState state)
            {
                try
                {
                    state.Reset();

                    if (Communicator.sendStateObjects.Count < 1000)
                    {
                        Communicator.sendStateObjects.Enqueue(state);
                    }
                }
                catch
                {
                }
            }

            internal void Reset()
            {
                socketInfo = null;
                socketQueue = null;
                element = defaultElement;
                socket = null;
            }
        }

        internal class ListenHandler : ServiceThread
        {
            public ListenHandler()
            {
            }

            public override void Start()
            {
                IPAddress thisAddress;
                IPEndPoint thisEndPoint;

                Socket socket;

                Socket listenSocket = null;

                string clientRouterName = string.Empty;

                if (configSettings.HubRoleSettings.ThisRouter.Nic == string.Empty)
                {
                    thisAddress = IPAddress.Any;
                }
                else
                {
                    IPHostEntry iph = Dns.GetHostEntry(configSettings.HubRoleSettings.ThisRouter.Nic);

                    thisAddress = null;

                    for (int i = 0; i < iph.AddressList.Length; i++)
                    {
                        if (configSettings.HubRoleSettings.ThisRouter.Nic == iph.AddressList[i].ToString())
                        {
                            thisAddress = iph.AddressList[i];

                            break;
                        }
                    }

                    if (thisAddress == null)
                    {
                        thisAddress = iph.AddressList[iph.AddressList.Length - 1];
                    }
                }

                thisEndPoint = new IPEndPoint(thisAddress, configSettings.HubRoleSettings.ThisRouter.Port);

                try
                {
                    listenSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                    listenSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                    listenSocket.NoDelay = true;
                    listenSocket.Bind(thisEndPoint);
                    listenSocket.Listen(20);

                    while (true)
                    {
                        clientRouterName = string.Empty;

                        socket = listenSocket.Accept();

                        socket.NoDelay = true;
                        socket.ReceiveTimeout = configSettings.HubRoleSettings.ThisRouter.Timeout;
                        socket.SendTimeout = configSettings.HubRoleSettings.ThisRouter.Timeout;

                        AcceptConnection(socket);
                    }
                }

                catch
                {
                    if (listenSocket != null)
                    {
                        Communicator.CloseSocket(listenSocket, clientRouterName);
                    }
                }
            }

            public void AcceptConnection(Socket socket)
            {
                PerformanceCounter threadSocketCounter;
                SynchronizationQueue<QueueElement> socketQueue;

                SocketInfo socketInfoIn = null;
                Thread commThread;

                try
                {
                    lock (Communicator.socketQueuesLock)
                    {
                        socketInfoIn = InitConnection(socket);
                    }
                }
                catch
                {
                    Communicator.CloseSocket(socket, "<unknown>");

                    socket = null;
                }

                if (socket != null)
                {
                    lock (Communicator.socketQueuesLock)
                    {
                        SocketInfo socketInfo;

                        if (Communicator.commSockets.TryGetValue(socketInfoIn.RouterName, out socketInfo) == true)
                        {
                            socketInfo.Sockets.Add(socket);
                        }
                        else
                        {
                            socketInfo = socketInfoIn;
                            Communicator.commSockets[socketInfo.RouterName] = socketInfo;
                        }

                        if (Communicator.socketQueues.TryGetValue(socketInfo.RouterName, out socketQueue) == false)
                        {
                            threadSocketCounter = new PerformanceCounter();
                            threadSocketCounter.InstanceLifetime = PerformanceCounterInstanceLifetime.Process;
                            threadSocketCounter.CategoryName = communicationCategoryName;
                            threadSocketCounter.CounterName = forwarderQueueSizeName;
                            threadSocketCounter.InstanceName = socketInfo.RouterName;
                            threadSocketCounter.ReadOnly = false;

                            socketQueue = new SynchronizationQueue<QueueElement>(threadSocketCounter);

                            Communicator.socketQueues[socketInfo.RouterName] = socketQueue;

                            socketQueue.InUse = true;
                        }
                        else
                        {
                            if (socketQueue.InUse != true)
                            {
                                socketQueue.InUse = true;

                                if (Communicator.deadSocketQueues.ContainsKey(socketInfo.RouterName) == true)
                                {
                                    Communicator.deadSocketQueues.Remove(socketInfo.RouterName);
                                }
                            }
                        }

                        commThread = new Thread(new ThreadStart(new CommunicationHandler(socket, socketInfo, socketQueue).ConnectionInStart));

                        commThread.Start();

                        if (socketInfo.UseToSend == true)
                        {
                            commThread = new Thread(new ThreadStart(new CommunicationHandler(socket, socketInfo, socketQueue).ConnectionOutStart));

                            commThread.Start();
                        }

                        if (socketInfo.Hub == false)
                        {
                            SubscriptionMgr.ResendSubscriptions(socketInfo.RouterName);
                        }
                    }
                }
            }

            private SocketInfo InitConnection(Socket socket)
            {
                SocketInfo socketInfoOut = new SocketInfo();
                SocketError socketError = SocketError.NoData;
                int totalBytesRead = 0;

                byte[] inResponse = new byte[1];
                byte[] outResponse = new byte[1];
                byte[] inBuffer = new byte[1000];

                byte[] inStream = new byte[1000];

                socketInfoOut.Sockets.Add(socket);

                try
                {
                    totalBytesRead = socket.Receive(inStream, 0, inStream.Length, SocketFlags.None, out socketError);
                }
                catch (SocketException e)
                {
                    Communicator.CloseSocket(socket, "<unknown>");
                    socket = null;

                    EventLog.WriteEntry("WspEventRouter", "Receive had Socket Exception with error code: " + e.ErrorCode.ToString(), EventLogEntryType.Warning);

                    return socketInfoOut;
                }
                catch
                {
                }

                if (socketError != SocketError.Success)
                {
                    Communicator.CloseSocket(socket, "<unknown>");
                    socket = null;

                    EventLog.WriteEntry("WspEventRouter", "Receive failed with bad return code: " + socketError.ToString(), EventLogEntryType.Warning);

                    return socketInfoOut;
                }

                UTF8Encoding uniEncoding = new UTF8Encoding();
                int position = 0;

                int preambleLength = BitConverter.ToInt32(inStream, position);
                position += sizeof(Int32);

                socketInfoOut.Hub = BitConverter.ToBoolean(inStream, position);
                position += sizeof(bool);

                int versionLength = BitConverter.ToInt32(inStream, position);
                position += sizeof(Int32);

                string versionIn = uniEncoding.GetString(inStream, position, versionLength);
                position += versionLength;

                int groupLength = BitConverter.ToInt32(inStream, position);
                position += sizeof(Int32);

                socketInfoOut.Group = uniEncoding.GetString(inStream, position, groupLength);
                position += groupLength;

                int routerNameLength = BitConverter.ToInt32(inStream, position);
                position += sizeof(Int32);

                socketInfoOut.RouterName = uniEncoding.GetString(inStream, position, routerNameLength);

                if (preambleLength == totalBytesRead && string.Compare(version, versionIn, true) == 0)
                {
                    outResponse[0] = 1;
                }
                else
                {
                    outResponse[0] = 0;
                }

                socket.Send(outResponse, 0, 1, SocketFlags.None, out socketError);

                if (socketInfoOut.Hub == false || string.Compare(socketInfoOut.Group, configSettings.EventRouterSettings.Group, true) == 0)
                {
                    socketInfoOut.UseToSend = true;
                }
                else
                {
                    socketInfoOut.UseToSend = false;
                }

                if (socketError != SocketError.Success || outResponse[0] == 0)
                {
                    Communicator.CloseSocket(socket, socketInfoOut.RouterName);
                    socket = null;

                    return socketInfoOut;
                }

                return socketInfoOut;
            }
        }

        internal class CommunicationHandler : ServiceThread
        {
            private Socket socket;
            private SocketInfo clientSocketInfo;
            private SynchronizationQueue<QueueElement> socketQueue;

            public CommunicationHandler(Socket socket, SocketInfo clientSocketInfo, SynchronizationQueue<QueueElement> socketQueue)
            {
                this.socket = socket;
                this.clientSocketInfo = clientSocketInfo;
                this.socketQueue = socketQueue;
            }

            public override void Start()
            {
                throw new Exception("The method or operation is not implemented.");
            }

            public void ConnectionInStart()
            {
                SocketAsyncEventArgs receiveEventArg;
                ReceiveState state = new ReceiveState();

                state.Reset();

                state.socket = this.socket;
                state.socketInfo = this.clientSocketInfo;
                state.socketQueue = this.socketQueue;

                receiveEventArg = new SocketAsyncEventArgs();
                receiveEventArg.Completed += new EventHandler<SocketAsyncEventArgs>(receiveEventArg_Completed);
                receiveEventArg.UserToken = state;
                receiveEventArg.SetBuffer(state.buffer, 0, state.buffer.Length);

                if (state.socket.ReceiveAsync(receiveEventArg) == false)
                {
                    ProcessReceive(receiveEventArg);
                }

                return;
            }

            private void receiveEventArg_Completed(object sender, SocketAsyncEventArgs receiveEventArg)
            {
                ProcessReceive(receiveEventArg);
            }

            private static void ProcessReceive(SocketAsyncEventArgs receiveEventArg)
            {
                ReceiveState state = (ReceiveState)receiveEventArg.UserToken;
                Socket socket = state.socket;
                SocketError socketError = receiveEventArg.SocketError;

                Guid eventType = Guid.Empty;
                string originatingRouterName = string.Empty;
                string inRouterName = string.Empty;

                int bytesRead = 0;
                int bytesProcessed = 0;
                int remainingLength = 0;

                if (receiveEventArg.BytesTransferred <= 0 || receiveEventArg.SocketError != SocketError.Success)
                {
                    Communicator.CloseSocket(socket, state.socketInfo.RouterName);

                    return;
                }

                bytesRead = receiveEventArg.BytesTransferred;

                while (bytesProcessed < bytesRead)
                {
                    if (state.currentReceiveLength == 0)
                    {
                        bytesProcessed += GetReceiveLength(state, bytesProcessed, bytesRead);

                        continue;
                    }

                    remainingLength = state.currentReceiveLength - state.currentProcessedLength;

                    if (bytesRead - bytesProcessed >= remainingLength)
                    {
                        byte[] inBuffer = new byte[remainingLength];

                        Buffer.BlockCopy(state.buffer, bytesProcessed, inBuffer, 0, remainingLength);

                        state.buffers.Add(inBuffer);

                        state.totalBytesRead = state.totalBytesRead + remainingLength;

                        bytesProcessed = bytesProcessed + remainingLength;
                        state.currentProcessedLength = state.currentProcessedLength + remainingLength;
                    }
                    else
                    {
                        byte[] inBuffer = new byte[bytesRead - bytesProcessed];

                        Buffer.BlockCopy(state.buffer, bytesProcessed, inBuffer, 0, bytesRead - bytesProcessed);

                        state.buffers.Add(inBuffer);

                        state.totalBytesRead = state.totalBytesRead + bytesRead - bytesProcessed;

                        state.currentProcessedLength = state.currentProcessedLength + bytesRead - bytesProcessed;
                        bytesProcessed = bytesRead;
                    }

                    if (state.currentProcessedLength == state.currentReceiveLength)
                    {
                        byte[] serializedEvent = ConcatArrayList(state.buffers);

                        WspEvent wspEvent = WspEvent.ChangeInRouterName(serializedEvent, state.socketInfo.routerNameEncoded, state.socketInfo.RouterName);

                        if (String.Compare(wspEvent.OriginatingRouterName, Router.localRouterName, true) != 0)
                        {
                            QueueElement element = new QueueElement();

                            element.WspEvent = wspEvent;

                            for(int tries = 0; tries < 10; tries++)
                            {
                                try
                                {
                                    rePublisherQueue.Enqueue(element);

                                    break;
                                }
                                catch (Exception e)
                                {
                                    if (tries == 10)
                                    {
                                        EventLog.WriteEntry("WspEventRouter", "Event dropped: " + e.ToString(), EventLogEntryType.Error);
                                    }
                                    else
                                    {
                                        EventLog.WriteEntry("WspEventRouter", e.ToString(), EventLogEntryType.Warning);
                                    }
                                }
                            }
                        }

                        state.Reset();
                    }
                }

                if (state.socket.ReceiveAsync(receiveEventArg) == false)
                {
                    ProcessReceive(receiveEventArg);
                }

                return;
            }

            private static int GetReceiveLength(ReceiveState state, int bytesProcessed, int bytesRead)
            {
                if (bytesRead - bytesProcessed < 4 - state.currentReceiveLengthBytesRead)
                {
                    Buffer.BlockCopy(state.buffer, bytesProcessed,
                        state.currentReceiveLengthBytes, state.currentReceiveLengthBytesRead, bytesRead - bytesProcessed);

                    state.currentReceiveLengthBytesRead = state.currentReceiveLengthBytesRead + bytesRead - bytesProcessed;

                    return bytesRead - bytesProcessed;
                }

                int returnLength = 4 - state.currentReceiveLengthBytesRead;

                Buffer.BlockCopy(state.buffer, bytesProcessed,
                    state.currentReceiveLengthBytes, state.currentReceiveLengthBytesRead, 4 - state.currentReceiveLengthBytesRead);

                state.currentReceiveLengthBytesRead = 4;

                state.currentReceiveLength = BitConverter.ToInt32(state.currentReceiveLengthBytes, 0);

                return returnLength;
            }

            public void ConnectionOutStart()
            {
                SendState state = SendState.Create(this.clientSocketInfo);
                RefCount refCount;

                if (state == null)
                {
                    return;
                }

                lock (Communicator.sendThreadQueueLock)
                {
                    if (Communicator.serverSendThreads.TryGetValue(this.clientSocketInfo.RouterName, out refCount) == false)
                    {
                        refCount = new RefCount();
                        Communicator.serverSendThreads[this.clientSocketInfo.RouterName] = refCount;
                    }
                }

                if (Interlocked.Increment(ref refCount.Count) > 2)
                {
                    Interlocked.Decrement(ref refCount.Count);
                }
                else
                {
                    ProcessSend(state);
                }
            }

            /// <summary>
            /// Callback by threadpool
            /// </summary>
            internal static void SendCallback(object routerName)
            {
                try
                {
                    SendState state = SendState.Create((string)routerName);

                    if (state == null)
                    {
                        RefCount.Decrement((string)routerName);

                        return;
                    }

                    ProcessSend(state);
                }
                catch
                {
                    RefCount.Decrement((string)routerName);
                }
            }

            internal static void ProcessSend(SendState state)
            {
                QueueElement element;
                QueueElement newElement = new QueueElement();
                bool elementRetrieved;
                ArraySegment<byte> bufferLengthOut = new ArraySegment<byte>(BitConverter.GetBytes(0));
                ArraySegment<byte> bufferDataOut;
                List<ArraySegment<byte>> buffersOut;
                Socket currSocket;
                string routerName = state.socketInfo.RouterName;

                try
                {
                    while (true)
                    {
                        lock (Communicator.socketQueuesLock)
                        {
                            if (state.socketInfo.Sockets.Count == 0)
                            {
                                currSocket = null;
                            }
                            else
                            {
                                if (state.socketInfo.Sockets.Count == 1)
                                {
                                    currSocket = state.socketInfo.Sockets[0];
                                }
                                else
                                {
                                    Random randObj = new Random();
                                    currSocket = state.socketInfo.Sockets[randObj.Next(state.socketInfo.Sockets.Count)];
                                }

                                if (currSocket.Connected == false)
                                {
                                    currSocket = null;

                                    for (int i = 0; i < state.socketInfo.Sockets.Count; i++)
                                    {
                                        if (state.socketInfo.Sockets[i].Connected == true)
                                        {
                                            currSocket = state.socketInfo.Sockets[i];

                                            break;
                                        }
                                    }
                                }
                            }
                        }

                        if (currSocket == null)
                        {
                            RefCount.Decrement(state.socketInfo.RouterName);
                            SendState.Recycle(state);
                            return;
                        }

                        try
                        {
                            if (currSocket.Connected == false)
                            {
                                RefCount.Decrement(state.socketInfo.RouterName);
                                SendState.Recycle(state);
                                return;
                            }

                            element = state.socketQueue.Dequeue(10000);

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
                            state.element = element;
                            state.socket = currSocket;

                            buffersOut = new List<ArraySegment<byte>>(2);

                            Buffer.BlockCopy(BitConverter.GetBytes(element.WspEvent.SerializedEvent.Length), 0, bufferLengthOut.Array, 0, sizeof(Int32));
                            buffersOut.Add(bufferLengthOut);

                            bufferDataOut = new ArraySegment<byte>(element.WspEvent.SerializedEvent);
                            buffersOut.Add(bufferDataOut);

                            SocketAsyncEventArgs sendEventArg = sendEventArg = new SocketAsyncEventArgs();
                            sendEventArg.Completed += new EventHandler<SocketAsyncEventArgs>(sendEventArg_Completed);

                            sendEventArg.BufferList = buffersOut;
                            sendEventArg.UserToken = state;

                            SendState newState = SendState.Create(state.socketInfo);
                            state = newState;

                            bool willRaiseEvent = currSocket.SendAsync(sendEventArg);
                            if (!willRaiseEvent)
                            {
                                SendCompleted(sendEventArg);
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    EventLog.WriteEntry("WspEventRouter", e.ToString(), EventLogEntryType.Warning);
                }

                RefCount.Decrement(routerName);
                SendState.Recycle(state);
                return;
            }

            private static void sendEventArg_Completed(object sender, SocketAsyncEventArgs sendEventArg)
            {
                SendCompleted(sendEventArg);
            }

            private static void SendCompleted(SocketAsyncEventArgs sendEventArg)
            {
                SendState state = (SendState)sendEventArg.UserToken;

                if (sendEventArg.SocketError != SocketError.Success)
                {
                    try
                    {
                        state.socketQueue.Enqueue(state.element);
                    }
                    catch (Exception e)
                    {
                        EventLog.WriteEntry("WspEventRouter", e.ToString(), EventLogEntryType.Warning);
                    }

                    Communicator.CloseSocket(state.socket, state.socketInfo.RouterName);
                }

                sendEventArg.Dispose();

                SendState.Recycle(state);
            }
        }

        internal class DistributeHandler : ServiceThread
        {
            private static Dictionary<string, bool> sentRoutes = new Dictionary<string, bool>();

            public DistributeHandler()
            {
            }

            public override void Start()
            {
                QueueElement element;
                QueueElement newElement = new QueueElement();
                bool elementRetrieved;

                try
                {
                    Manager.ThreadInitialize.Release();
                }
                catch
                {
                    // If the thread is restarted, this could throw an exception but just ignore
                }

                try
                {
                    while (true)
                    {
                        try
                        {
                            element = forwarderQueue.Dequeue();

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
                        catch (System.TimeoutException)
                        {
                            element = newElement;
                            elementRetrieved = false;
                        }

                        if (elementRetrieved == true)
                        {
                            if (element.WspEvent.EventType == Subscription.SubscriptionEvent ||
                                element.WspEvent.EventType == configSettings.EventRouterSettings.MgmtGuid ||
                                element.WspEvent.EventType == configSettings.EventRouterSettings.CmdGuid)
                            {
                                DistributeSpecialEvents(element);
                            }
                            else
                            {
                                DistributeNormalEvents(element);
                            }
                        }
                    }
                }

                catch (Exception e)
                {
                    EventLog.WriteEntry("WspEventRouter", e.ToString(), EventLogEntryType.Warning);

                    return;
                }
            }

            private static void DistributeNormalEvents(QueueElement element)
            {
                RouteDetail subscriptionDetail;
                FilterSummary filterSummary;

                if (SubscriptionMgr.filteredSubscriptions.TryGetValue(element.WspEvent.EventType, out filterSummary) == false)
                {
                    filterSummary = null;
                }

                if (SubscriptionMgr.generalSubscriptions.TryGetValue(element.WspEvent.EventType, out subscriptionDetail) == false)
                {
                    subscriptionDetail = null;
                }

                sentRoutes.Clear();
                
                if (filterSummary != null || subscriptionDetail != null)
                {
                    lock (SubscriptionMgr.subscriptionsLock)
                    {
                        if (subscriptionDetail != null)
                        {
                            foreach (string routerName in subscriptionDetail.Routes.Keys)
                            {
                                if (sentRoutes.ContainsKey(routerName) == false)
                                {
                                    sentRoutes.Add(routerName, true);

                                    DistributeEvent(element, routerName);
                                }
                            }
                        }

                        if (filterSummary != null)
                        {
                            foreach (string filter in filterSummary.Filters.Keys)
                            {
                                if (filterSummary.Filters[filter].UniqueRoutes.Routes.Count > 0)
                                {
                                    if (filterSummary.Filters[filter].filterMethod(element.WspEvent) == true)
                                    {
                                        foreach (string routerName in filterSummary.Filters[filter].UniqueRoutes.Routes.Keys)
                                        {
                                            if (sentRoutes.ContainsKey(routerName) == false)
                                            {
                                                sentRoutes.Add(routerName, true);

                                                DistributeEvent(element, routerName);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                return;
            }

            private static void DistributeEvent(QueueElement element, string routerName)
            {
                SynchronizationQueue<QueueElement> queue;
                SocketInfo socketInfo;

                if (string.IsNullOrEmpty(routerName) == false &&
                    string.Compare(routerName, Router.LocalRouterName, true) != 0 &&
                    string.Compare(routerName, element.WspEvent.InRouterName, true) != 0)
                {
                    if (Router.hubRole == false)
                    {
                        // forward all if this is a node
                    }
                    else
                    {
                        if (Communicator.commSockets.ContainsKey(routerName) == true)
                        {
                            socketInfo = Communicator.commSockets[routerName];
                        }
                        else
                        {
                            if (Communicator.deadSocketQueues.ContainsKey(routerName) == true)
                            {
                                socketInfo = Communicator.deadSocketQueues[routerName];
                            }
                            else
                            {
                                return;
                            }
                        }

                        if (socketInfo.Hub == false)
                        {
                            // forward to all nodes
                        }
                        else
                        {
                            if (element.Source == EventSource.FromLocal || element.Source == EventSource.FromNode)
                            {
                                if (string.Compare(socketInfo.Group, configSettings.EventRouterSettings.Group, true) == 0)
                                {
                                    // forward to all hubs in group
                                }
                                else
                                {
                                    if (socketInfo.UseToSend == true)
                                    {
                                        // forward to these peers
                                    }
                                    else
                                    {
                                        return;  // don't forward to these peers
                                    }
                                }
                            }
                            else
                            {
                                if (string.Compare(socketInfo.Group, configSettings.EventRouterSettings.Group, true) == 0)
                                {
                                    if (element.Source == EventSource.FromPeer)
                                    {
                                        // forward to all hubs in group
                                    }
                                    else
                                    {
                                        return;  // don't forward to other hubs
                                    }
                                }
                                else
                                {
                                    return;  // don't forward to these peers
                                }
                            }
                        }
                    }

                    if (Communicator.socketQueues.TryGetValue(routerName, out queue) == true)
                    {
                        queue.Enqueue(element);

                        RefCount refCount;

                        if (Communicator.serverSendThreads.TryGetValue(routerName, out refCount) == false)
                        {
                            lock (Communicator.sendThreadQueueLock)
                            {
                                if (Communicator.serverSendThreads.ContainsKey(routerName) == false)
                                {
                                    refCount = new RefCount();
                                    Communicator.serverSendThreads[routerName] = refCount;
                                }
                            }
                        }

                        if (Interlocked.Increment(ref refCount.Count) > 2)
                        {
                            Interlocked.Decrement(ref refCount.Count);
                        }
                        else
                        {
                            ThreadPool.QueueUserWorkItem(new WaitCallback(CommunicationHandler.SendCallback), routerName);
                        }
                    }
                }
            }

            private static void DistributeSpecialEvents(QueueElement element)
            {
                SynchronizationQueue<QueueElement> queue;
                SocketInfo socketInfo;

                lock (Communicator.socketQueuesLock)
                {
                    foreach (string routerName in Communicator.socketQueues.Keys)
                    {
                        if (string.Compare(element.WspEvent.InRouterName, routerName, true) != 0)
                        {
                            if (element.Source == EventSource.FromLocal || element.Source == EventSource.FromNode)
                            {
                                // forward to all
                            }
                            else
                            {
                                if (Communicator.commSockets.ContainsKey(routerName) == true)
                                {
                                    socketInfo = Communicator.commSockets[routerName];
                                }
                                else
                                {
                                    if (Communicator.deadSocketQueues.ContainsKey(routerName) == true)
                                    {
                                        socketInfo = Communicator.deadSocketQueues[routerName];
                                    }
                                    else
                                    {
                                        continue;
                                    }
                                }

                                if (socketInfo.Hub == false)
                                {
                                    // forward to all nodes
                                }
                                else
                                {
                                    if (string.Compare(socketInfo.Group, configSettings.EventRouterSettings.Group, true) == 0 ||
                                        element.Source == EventSource.FromPeer)
                                    {
                                        continue;  // don't forward to other hubs of same group or if source is from a peer
                                    }
                                    else
                                    {
                                        // forward to all peers
                                    }
                                }
                            }

                            queue = Communicator.socketQueues[routerName];

                            queue.Enqueue(element);

                            RefCount refCount;

                            if (Communicator.serverSendThreads.TryGetValue(routerName, out refCount) == false)
                            {
                                lock (Communicator.sendThreadQueueLock)
                                {
                                    if (Communicator.serverSendThreads.ContainsKey(routerName) == false)
                                    {
                                        refCount = new RefCount();
                                        Communicator.serverSendThreads[routerName] = refCount;
                                    }
                                }
                            }

                            if (Interlocked.Increment(ref refCount.Count) > 2)
                            {
                                Interlocked.Decrement(ref refCount.Count);
                            }
                            else
                            {
                                ThreadPool.QueueUserWorkItem(new WaitCallback(CommunicationHandler.SendCallback), routerName);
                            }
                        }
                    }
                }
                return;
            }
        }
    }
}
