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

namespace Microsoft.WebSolutionsPlatform.Event
{
    public partial class Router : ServiceBase
    {
        internal class Communicator : ServiceThread
        {
            internal static Thread receiveServerThread;
            internal static Thread parentInConnection;
            internal static Thread parentOutConnection;
            internal static Thread distributeThread;

            internal static Dictionary<string, Thread> receiveThreads = new Dictionary<string, Thread>();
            internal static Dictionary<string, Thread> forwardThreads = new Dictionary<string, Thread>();

            internal static Dictionary<string, SynchronizationQueue<QueueElement>> threadQueues =
                new Dictionary<string, SynchronizationQueue<QueueElement>>();

            public Communicator()
            {
            }

            public override void Start()
            {
                int i;
                List<string> removeItems = new List<string>();

                while (true)
                {
                    try
                    {
                        if (thisPort != 0)
                        {
                            if (receiveServerThread == null || receiveServerThread.ThreadState == System.Threading.ThreadState.Stopped)
                            {
                                if (receiveServerThread != null &&
                                    receiveServerThread.ThreadState == System.Threading.ThreadState.Stopped)
                                {
                                    receiveServerThread.Abort();
                                }

                                receiveServerThread = new Thread(new ThreadStart(new ReceiveServer().Start));

                                receiveServerThread.Start();
                            }
                        }

                        if (distributeThread == null || distributeThread.ThreadState == System.Threading.ThreadState.Stopped)
                        {
                            if (distributeThread != null && distributeThread.ThreadState == System.Threading.ThreadState.Stopped)
                            {
                                distributeThread.Abort();
                            }

                            distributeThread = new Thread(new ThreadStart(new DistributeHandler().Start));

                            distributeThread.Start();
                        }

                        if (Router.parentRoute != null && 
                            (parentInConnection == null || parentInConnection.ThreadState == System.Threading.ThreadState.Stopped))
                        {
                            if (parentInConnection != null)
                            {
                                lock (receiveThreads)
                                {
                                    parentInConnection.Abort();

                                    receiveThreads.Remove(Router.parentRoute.RouterName);
                                }
                            }

                            parentInConnection = new Thread(new ThreadStart(new CommunicationHandler(null, 1).Start));

                            parentInConnection.Start();
                        }

                        if (Router.parentRoute != null && 
                            (parentOutConnection == null || parentOutConnection.ThreadState == System.Threading.ThreadState.Stopped))
                        {
                            if (parentOutConnection != null)
                            {
                                lock (forwardThreads)
                                {
                                    forwardThreads[Router.parentRoute.RouterName] = parentOutConnection;
                                }
                            }

                            parentOutConnection = new Thread(new ThreadStart(new CommunicationHandler(null, 2).Start));

                            parentOutConnection.Start();
                        }

                        lock (receiveThreads)
                        {
                            foreach(string routerName in receiveThreads.Keys)
                            {
                                if (receiveThreads[routerName].ThreadState == System.Threading.ThreadState.Stopped)
                                {
                                    receiveThreads[routerName].Abort();

                                    removeItems.Add(routerName);
                                }
                            }

                            if (removeItems.Count > 0)
                            {
                                for (i = 0; i < removeItems.Count; i++)
                                {
                                    receiveThreads.Remove(removeItems[i]);
                                }

                                removeItems.Clear();
                            }
                        }

                        lock (forwardThreads)
                        {
                            foreach (string routerName in forwardThreads.Keys)
                            {
                                if (forwardThreads[routerName].ThreadState == System.Threading.ThreadState.Stopped)
                                {
                                    forwardThreads[routerName].Abort();

                                    removeItems.Add(routerName);
                                }
                            }

                            if (removeItems.Count > 0)
                            {
                                for (i = 0; i < removeItems.Count; i++)
                                {
                                    forwardThreads.Remove(removeItems[i]);
                                }

                                removeItems.Clear();
                            }
                        }

                        Thread.Sleep(10000);
                    }

                    catch (ThreadAbortException)
                    {
                        return;
                    }

                    catch (Exception e)
                    {
                        EventLog.WriteEntry("WspEventRouter", e.ToString(), EventLogEntryType.Warning);
                    }
                }
            }
        }

        internal class ReceiveServer : ServiceThread
        {
            private IPAddress thisAddress;
            private IPEndPoint thisEndPoint;

            public ReceiveServer()
            {
                if (thisNic == string.Empty)
                {
                    thisAddress = Dns.GetHostEntry(LocalRouterName).AddressList[Dns.GetHostEntry(LocalRouterName).AddressList.Length - 1];
                }
                else
                {
                    IPHostEntry iph = Dns.GetHostEntry(thisNic);

                    thisAddress = null;

                    for (int i = 0; i < iph.AddressList.Length; i++)
                    {
                        if (thisNic == iph.AddressList[i].ToString())
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

                thisEndPoint = new IPEndPoint(thisAddress, thisPort);
            }

            public override void Start()
            {
                Socket server = null;

                try
                {
                    server = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                    server.NoDelay = true;
                    server.Bind(thisEndPoint);
                    server.Listen(20);

                    while (true)
                    {
                        Socket client = server.Accept();

                        client.NoDelay = true;
                        client.ReceiveTimeout = thisTimeout;
                        client.SendTimeout = thisTimeout;

                        Thread receiveThread = new Thread(new ThreadStart(new CommunicationHandler(client, 0).Start));

                        receiveThread.Start();
                    }
                }

                catch
                {
                    if (server != null)
                    {
                        try
                        {
                            server.Shutdown(SocketShutdown.Both);
                        }

                        catch (Exception e)
                        {
                            EventLog.WriteEntry("WspEventRouter", e.ToString(), EventLogEntryType.Warning);
                        }

                        try
                        {
                            server.Close();
                        }

                        catch (Exception e)
                        {
                            EventLog.WriteEntry("WspEventRouter", e.ToString(), EventLogEntryType.Warning);
                        }
                    }
                }
            }
        }

        internal class ReceiveStateObject
        {
            public Socket client;
            public byte[] buffer;
            public ArrayList buffers;
            public int totalBytesRead;
            public ManualResetEvent receiveDone;
            public int currentReceiveLength;
            public byte[] currentReceiveLengthBytes;
            public int currentReceiveLengthBytesRead;
            public int currentProcessedLength;
            public string clientRouterName = string.Empty;

            internal ReceiveStateObject()
            {
                buffer = new byte[averageEventSize];
                buffers = new ArrayList(10);
                receiveDone = new ManualResetEvent(false);
                currentReceiveLengthBytes = new byte[4];
            }

            internal void Reset()
            {
                totalBytesRead = 0;
                currentReceiveLength = 0;
                currentReceiveLengthBytesRead = 0;
                currentProcessedLength = 0;

                buffers.Clear();
                receiveDone.Reset();
            }
        }

        internal class CommunicationHandler : ServiceThread
        {
            private static ManualResetEvent sendDone = new ManualResetEvent(false);

            private static PrefixStream prefixStream = new PrefixStream();
            private static BinaryReader binReader = new BinaryReader(prefixStream, Encoding.UTF8);

            private bool msgInHandler;
            private Socket client;
            private int commType;
            private string clientRouterName = string.Empty;

            internal PerformanceCounter threadQueueCounter;

            internal SynchronizationQueue<QueueElement> threadQueue;

            public CommunicationHandler(Socket client, int commType)
            {
                this.client = client;
                this.commType = commType;
            }

            public override void Start()
            {
                int inByte = -1;
                byte[] preamble;
                byte[] inResponse = new byte[1];
                byte[] outResponse = new byte[1];
                ReceiveStateObject receiveState = new ReceiveStateObject();

                try
                {
                    if (commType == 1)
                    {
                        clientRouterName = Router.parentRoute.RouterName;

                        lock (Communicator.receiveThreads)
                        {
                            Communicator.receiveThreads[clientRouterName] = Thread.CurrentThread;
                        }

                        client = ConnectSocket(Router.parentRoute.RouterName, Router.parentRoute.Port);

                        if (client == null)
                        {
                            return;
                        }

                        client.NoDelay = true;
                        client.ReceiveTimeout = Router.parentRoute.Timeout;
                        client.SendTimeout = Router.parentRoute.Timeout;

                        preamble = new byte[routerNameEncoded.Length + 5];

                        Buffer.BlockCopy(BitConverter.GetBytes((Int32)preamble.Length), 0, preamble, 0, 4);
                        preamble[4] = 0;
                        Buffer.BlockCopy(routerNameEncoded, 0, preamble, 5, routerNameEncoded.Length);

                        client.Send(preamble);

                        client.Receive(inResponse);

                        InHandler(new ReceiveStateObject());

                        return;
                    }

                    if (commType == 2)
                    {
                        clientRouterName = Router.parentRoute.RouterName;

                        lock (Communicator.forwardThreads)
                        {
                            Communicator.forwardThreads[clientRouterName] = Thread.CurrentThread;

                            if (Communicator.threadQueues.ContainsKey(clientRouterName) == true)
                            {
                                threadQueue = Communicator.threadQueues[clientRouterName];
                            }
                            else
                            {
                                threadQueueCounter = new PerformanceCounter(communicationCategoryName,
                                    forwarderQueueSizeName, clientRouterName, false);

                                threadQueue = new SynchronizationQueue<QueueElement>(threadQueueCounter);
                                Communicator.threadQueues[clientRouterName] = threadQueue;
                            }
                        }

                        client = ConnectSocket(Router.parentRoute.RouterName, Router.parentRoute.Port);

                        if (client == null)
                        {
                            return;
                        }

                        client.NoDelay = true;
                        client.ReceiveTimeout = Router.parentRoute.Timeout;
                        client.SendTimeout = Router.parentRoute.Timeout;

                        preamble = new byte[routerNameEncoded.Length + 5];
                        Buffer.BlockCopy(BitConverter.GetBytes((Int32)preamble.Length), 0, preamble, 0, 4);
                        preamble[4] = 1;
                        Buffer.BlockCopy(routerNameEncoded, 0, preamble, 5, routerNameEncoded.Length);

                        client.Send(preamble);

                        client.Receive(inResponse);

                        SubscriptionMgr.ResendSubscriptions();

                        OutHandler();

                        return;
                    }

                    byte[] inStream = new byte[1000];

                    receiveState.client = client;
                    receiveState.clientRouterName = clientRouterName;
                    receiveState.buffer = inStream;
                    receiveState.receiveDone.Reset();

                    client.BeginReceive(receiveState.buffer, 0, receiveState.buffer.Length, SocketFlags.None,
                        InitialReceiveCallback, receiveState);

                    receiveState.receiveDone.WaitOne();

                    int preambleLength = BitConverter.ToInt32(receiveState.buffer, 0);

                    inByte = receiveState.buffer[4];

                    UnicodeEncoding uniEncoding = new UnicodeEncoding();
                    clientRouterName = uniEncoding.GetString(receiveState.buffer, 5, preambleLength - 5);

                    if (preambleLength == receiveState.totalBytesRead)
                    {
                        outResponse[0] = 1;
                    }
                    else
                    {
                        outResponse[0] = 0;
                    }

                    client.Send(outResponse, 1, SocketFlags.None);

                    msgInHandler = inByte == 1 ? true : false;

                    if (msgInHandler == true)
                    {
                        lock (Communicator.receiveThreads)
                        {
                            Communicator.receiveThreads[clientRouterName] = Thread.CurrentThread;
                        }

                        InHandler(new ReceiveStateObject());
                    }
                    else
                    {
                        lock (Communicator.forwardThreads)
                        {
                            Communicator.forwardThreads[clientRouterName] = Thread.CurrentThread;

                            if (Communicator.threadQueues.ContainsKey(clientRouterName) == true)
                            {
                                threadQueue = Communicator.threadQueues[clientRouterName];
                            }
                            else
                            {
                                threadQueueCounter = new PerformanceCounter(communicationCategoryName,
                                    forwarderQueueSizeName, clientRouterName, false);

                                threadQueue = new SynchronizationQueue<QueueElement>(threadQueueCounter);
                                Communicator.threadQueues[clientRouterName] = threadQueue;
                            }
                        }

                        SubscriptionMgr.ResendSubscriptions();

                        OutHandler();
                    }
                }
                catch
                {
                    try
                    {
                        client.Shutdown(SocketShutdown.Both);
                        client.Close();
                    }

                    catch (ObjectDisposedException)
                    {
                        // Intentionally left blank. This can happen when a child disconnects.
                    }

                    catch (Exception e)
                    {
                        EventLog.WriteEntry("WspEventRouter", e.ToString(), EventLogEntryType.Warning);
                    }

                    client = null;

                    return;
                }
            }

            private static void InitialReceiveCallback(IAsyncResult ar)
            {
                ReceiveStateObject receiveState = (ReceiveStateObject)ar.AsyncState;
                Socket client = receiveState.client;

                receiveState.totalBytesRead = client.EndReceive(ar);

                receiveState.receiveDone.Set();
            }

            private void InHandler(ReceiveStateObject state)
            {
                state.client = this.client;
                state.clientRouterName = this.clientRouterName;

                state.Reset();

                state.client.BeginReceive(state.buffer, 0, averageEventSize, SocketFlags.None,
                    new AsyncCallback(ReceiveCallback), state);

                state.receiveDone.WaitOne();

                state.client.Shutdown(SocketShutdown.Both);
                state.client.Close();
            }

            private static void ReceiveCallback(IAsyncResult ar)
            {
                ReceiveStateObject state = (ReceiveStateObject)ar.AsyncState;
                Socket client = state.client;

                Guid eventType = Guid.Empty;
                string originatingRouterName = string.Empty;
                string inRouterName = string.Empty;

                int bytesRead;
                int bytesProcessed = 0;
                int remainingLength = 0;

                if (client.Connected == true)
                {
                    try
                    {
                        bytesRead = client.EndReceive(ar);
                    }
                    catch
                    {
                        try
                        {
                            state.client.Shutdown(SocketShutdown.Both);
                            state.client.Close();
                        }

                        catch (Exception e)
                        {
                            EventLog.WriteEntry("WspEventRouter", e.ToString(), EventLogEntryType.Warning);
                        }

                        state.receiveDone.Set();

                        return;
                    }
                }
                else
                {
                    try
                    {
                        state.client.Shutdown(SocketShutdown.Both);
                        state.client.Close();
                    }

                    catch (Exception e)
                    {
                        EventLog.WriteEntry("WspEventRouter", e.ToString(), EventLogEntryType.Warning);
                    }

                    state.receiveDone.Set();

                    return;
                }

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
                        eventType = Guid.Empty;
                        originatingRouterName = string.Empty;
                        inRouterName = string.Empty;

                        lock (prefixStream)
                        {
                            prefixStream.Position = 0;
                            Buffer.BlockCopy((byte[])state.buffers[0], 0, prefixStream.prefixBuffer, 0,
                                ((byte[])state.buffers[0]).Length < prefixStream.prefixLength ? ((byte[])state.buffers[0]).Length : prefixStream.prefixLength);

                            originatingRouterName = binReader.ReadString();
                            inRouterName = binReader.ReadString(); // This is the old InRouterName
                            eventType = new Guid(binReader.ReadString());
                        }

                        if (String.Compare(originatingRouterName, Router.localRouterName, false) != 0)
                        {
                            Router.channelDictionary[originatingRouterName] = state.clientRouterName;

                            QueueElement element = new QueueElement();

                            element.SerializedEvent = ConcatArrayList(state.buffers);
                            element.SerializedLength = state.totalBytesRead;
                            element.EventType = eventType;
                            element.OriginatingRouterName = originatingRouterName;
                            element.InRouterName = state.clientRouterName;

                            rePublisherQueue.Enqueue(element);
                        }

                        state.Reset();
                    }
                }

                if (bytesRead > 0)
                {
                    client.BeginReceive(state.buffer, 0, averageEventSize, 0,
                        new AsyncCallback(ReceiveCallback), state);
                }
                else
                {
                    try
                    {
                        state.client.Shutdown(SocketShutdown.Both);
                        state.client.Close();
                    }

                    catch (Exception e)
                    {
                        EventLog.WriteEntry("WspEventRouter", e.ToString(), EventLogEntryType.Warning);
                    }

                    state.receiveDone.Set();

                    return;
                }
           }

            private static int GetReceiveLength(ReceiveStateObject state, int bytesProcessed, int bytesRead)
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

            private void OutHandler()
            {
                QueueElement element;
                QueueElement defaultElement = default(QueueElement);
                QueueElement newElement = new QueueElement();
                bool elementRetrieved;
                ArraySegment<byte> bufferLengthOut;
                ArraySegment<byte> bufferDataOut;
                List<ArraySegment<byte>> buffersOut;

                while (true)
                {
                    if (client.Connected == false)
                    {
                        try
                        {
                            client.Shutdown(SocketShutdown.Both);
                            client.Close();
                        }

                        catch (Exception e)
                        {
                            EventLog.WriteEntry("WspEventRouter", e.ToString(), EventLogEntryType.Warning);
                        }

                        client = null;

                        return;
                    }

                    try
                    {
                        element = threadQueue.Dequeue();

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
                        buffersOut = new List<ArraySegment<byte>>(2);

                        bufferLengthOut = new ArraySegment<byte>(BitConverter.GetBytes(element.SerializedEvent.Length));
                        buffersOut.Add(bufferLengthOut);

                        bufferDataOut = new ArraySegment<byte>(element.SerializedEvent);
                        buffersOut.Add(bufferDataOut);

                        try
                        {
                            client.BeginSend(buffersOut, SocketFlags.None, SendCallback, client);
                        }
                        catch
                        {
                            try
                            {
                                threadQueue.Enqueue(element);
                            }

                            catch (Exception e)
                            {
                                EventLog.WriteEntry("WspEventRouter", e.ToString(), EventLogEntryType.Warning);
                            }

                            try
                            {
                                client.Shutdown(SocketShutdown.Both);
                                client.Close();
                            }

                            catch (Exception e)
                            {
                                EventLog.WriteEntry("WspEventRouter", e.ToString(), EventLogEntryType.Warning);
                            }

                            client = null;

                            return;
                        }
                    }
                }
            }

            private static void SendCallback(IAsyncResult ar)
            {
                 try
                 {
                     ((Socket)ar.AsyncState).EndSend(ar);
                 }
                 catch
                 {
                     try
                     {
                         ((Socket)ar.AsyncState).Shutdown(SocketShutdown.Both);
                         ((Socket)ar.AsyncState).Close();
                     }

                     catch (Exception e)
                     {
                         EventLog.WriteEntry("WspEventRouter", e.ToString(), EventLogEntryType.Warning);
                     }
                 }
             }

            private static Socket ConnectSocket(string server, int port)
            {
                Socket s = null;
                IPHostEntry hostEntry = null;

                try
                {
                    // Get host related information.
                    hostEntry = Dns.GetHostEntry(server);

                    // Loop through the AddressList to obtain the supported AddressFamily. This is to avoid
                    // an exception that occurs when the host IP Address is not compatible with the address family
                    // (typical in the IPv6 case).
                    foreach (IPAddress address in hostEntry.AddressList)
                    {
                        IPEndPoint ipe = new IPEndPoint(address, port);
                        Socket tempSocket =
                            new Socket(ipe.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

                        tempSocket.Connect(ipe);

                        if (tempSocket.Connected)
                        {
                            s = tempSocket;
                            break;
                        }
                        else
                        {
                            continue;
                        }
                    }
                }
                catch
                {
                    s = null;
                }

                return s;
            }
        }

        internal class DistributeHandler : ServiceThread
        {
            public DistributeHandler()
            {
            }

            public override void Start()
            {
                string outRouterName;
                QueueElement element;
                QueueElement defaultElement = default(QueueElement);
                QueueElement newElement = new QueueElement();
                bool elementRetrieved;
                DoubleDictionary<Guid, Guid> eventDictionary;

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
                        catch (System.TimeoutException)
                        {
                            element = newElement;
                            elementRetrieved = false;
                        }

                        if (elementRetrieved == true)
                        {
                            if (channelDictionary.TryGetValue(element.OriginatingRouterName, out outRouterName) == false)
                            {
                                outRouterName = string.Empty;
                            }

                            foreach (string routerName in Communicator.threadQueues.Keys)
                            {
                                if (string.Compare(outRouterName, routerName, false) != 0)
                                {
                                    if (element.EventType == Event.SubscriptionEvent)
                                    {
                                        Communicator.threadQueues[routerName].Enqueue(element);
                                    }
                                    else
                                    {
                                        if (Router.routerDictionary.TryGetValue(routerName, out eventDictionary) == true)
                                        {
                                            if (eventDictionary.Dictionary1.ContainsKey(Guid.Empty) == true ||
                                                eventDictionary.Dictionary1.ContainsKey(element.EventType) == true)
                                            {
                                                Communicator.threadQueues[routerName].Enqueue(element);
                                            }
                                        }
                                    }
                                }
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
        }
    }
}
