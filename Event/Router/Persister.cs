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
using Microsoft.WebSolutionsPlatform.Event;
using Microsoft.WebSolutionsPlatform.Event.PubSubManager;

namespace Microsoft.WebSolutionsPlatform.Event
{
    public partial class Router : ServiceBase
    {
        internal class PersistEventInfo
        {
            internal bool InUse;
            internal bool Loaded;
            internal Guid PersistEventType;
            internal string CopyToFileDirectory;
            internal string TempFileDirectory;
            internal long MaxFileSize;
            internal long CopyIntervalTicks;
            internal long NextCopyTick;
            internal bool LocalOnly;
            internal string OutFileName;
            internal StreamWriter OutStream;
            internal string FieldTerminator;
            internal string RowTerminator;
            internal string HeaderRow;

            internal PersistEventInfo()
            {
                InUse = false;
                Loaded = false;
                HeaderRow = null;
                CopyIntervalTicks = 600000000;
                MaxFileSize = long.MaxValue - 1;
            }
        }

        internal class Persister : ServiceThread
        {
            internal static bool copyInProcess = false;
            internal static bool localOnly = true;

            internal static long lastConfigFileTick;
            internal static long nextConfigFileCheckTick = 0;

            internal static PublishManager pubMgr = null;

            internal static Dictionary<Guid, PersistEventInfo> persistEvents = new Dictionary<Guid, PersistEventInfo>();

            private Stack<PersistFileEvent> persistFileEvents;

            private long nextCopyTick = 0;

            private string fileNameBase = string.Empty;
            private string fileNameSuffix = string.Empty;

            private QueueElement element;
            private QueueElement defaultElement = default(QueueElement);

            public Persister()
            {
                fileNameBase = Dns.GetHostName() + @".Events.";
                fileNameSuffix = @".evt";

                persistFileEvents = new Stack<PersistFileEvent>();
            }

            public override void Start()
            {
                bool elementRetrieved;
                long currentTick;
                long fileTick;
                SerializationData serializationData;
                PersistEventInfo eventInfo;
                string eventFieldTerminator = @",";
                StreamWriter eventStream;

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
                    pubMgr = new PublishManager();

                    while (true)
                    {
                        currentTick = DateTime.UtcNow.Ticks;

                        if (currentTick > nextConfigFileCheckTick)
                        {
                            nextConfigFileCheckTick = currentTick + 300000000;

                            fileTick = Router.GetConfigFileTick();

                            if (fileTick != lastConfigFileTick)
                            {
                                nextCopyTick = 0;

                                foreach (PersistEventInfo eInfo in persistEvents.Values)
                                {
                                    eInfo.Loaded = false;
                                }

                                Router.LoadPersistConfig();

                                foreach (PersistEventInfo eInfo in persistEvents.Values)
                                {
                                    if (eInfo.Loaded == false)
                                    {
                                        eInfo.InUse = false;
                                        eInfo.NextCopyTick = currentTick - 1;
                                    }
                                }

                                lastConfigFileTick = fileTick;
                            }
                        }

                        try
                        {
                            element = persisterQueue.Dequeue();

                            if (element.Equals(defaultElement) == true)
                            {
                                elementRetrieved = false;
                            }
                            else
                            {
                                elementRetrieved = true;
                            }
                        }
                        catch (InvalidOperationException)
                        {
                            elementRetrieved = false;
                        }

                        currentTick = DateTime.UtcNow.Ticks;

                        if (currentTick > nextCopyTick)
                        {
                            nextCopyTick = long.MaxValue;

                            foreach (PersistEventInfo persistEventInfo in persistEvents.Values)
                            {
                                if (currentTick > persistEventInfo.NextCopyTick)
                                {
                                    persistEventInfo.NextCopyTick = currentTick + persistEventInfo.CopyIntervalTicks;

                                    if (persistEventInfo.OutStream != null)
                                    {
                                        persistEventInfo.OutStream.Close();

                                        SendPersistEvent(PersistFileState.Close, persistEventInfo, persistEventInfo.OutFileName);

                                        persistEventInfo.OutStream = null;
                                    }

                                    if (persistEventInfo.InUse == true)
                                    {
                                        persistEventInfo.OutFileName = persistEventInfo.TempFileDirectory + fileNameBase + persistEventInfo.NextCopyTick.ToString() + fileNameSuffix;

                                        if (File.Exists(persistEventInfo.OutFileName) == true)
                                            persistEventInfo.OutStream = new StreamWriter(File.Open(persistEventInfo.OutFileName, FileMode.Append, FileAccess.Write, FileShare.None), Encoding.Unicode);
                                        else
                                            persistEventInfo.OutStream = new StreamWriter(File.Open(persistEventInfo.OutFileName, FileMode.Create, FileAccess.Write, FileShare.None), Encoding.Unicode);

                                        SendPersistEvent(PersistFileState.Open, persistEventInfo, persistEventInfo.OutFileName);

                                        if (persistEventInfo.HeaderRow != null)
                                        {
                                            persistEventInfo.OutStream.Write(persistEventInfo.HeaderRow);
                                        }
                                    }
                                }

                                if (persistEventInfo.NextCopyTick < nextCopyTick)
                                {
                                    nextCopyTick = persistEventInfo.NextCopyTick;
                                }
                            }

                            if (copyInProcess == false)
                            {
                                Thread copyThread = new Thread(new ThreadStart(CopyFile));

                                copyInProcess = true;

                                copyThread.Start();
                            }
                        }

                        if (elementRetrieved == true)
                        {
                            eventInfo = persistEvents[element.EventType];

                            if (eventInfo.InUse == true)
                            {
                                eventFieldTerminator = eventInfo.FieldTerminator;

                                eventStream = eventInfo.OutStream;

                                if (eventStream == null)
                                {
                                    eventInfo.NextCopyTick = DateTime.UtcNow.Ticks + eventInfo.CopyIntervalTicks;

                                    eventInfo.OutFileName = eventInfo.TempFileDirectory + fileNameBase + eventInfo.NextCopyTick.ToString() + fileNameSuffix;

                                    if (File.Exists(eventInfo.OutFileName) == true)
                                        eventInfo.OutStream = new StreamWriter(File.Open(eventInfo.OutFileName, FileMode.Append, FileAccess.Write, FileShare.None), Encoding.Unicode);
                                    else
                                        eventInfo.OutStream = new StreamWriter(File.Open(eventInfo.OutFileName, FileMode.Create, FileAccess.Write, FileShare.None), Encoding.Unicode);

                                    eventStream = eventInfo.OutStream;

                                    if (eventInfo.NextCopyTick < nextCopyTick)
                                    {
                                        nextCopyTick = eventInfo.NextCopyTick;
                                    }

                                    SendPersistEvent(PersistFileState.Open, eventInfo, eventInfo.OutFileName);
                                }

                                if (eventInfo.HeaderRow == null)
                                {
                                    StringBuilder sb = new StringBuilder();

                                    serializationData = new SerializationData(element.SerializedEvent);

                                    serializationData.GetOriginatingRouterName();
                                    serializationData.GetInRouterName();
                                    serializationData.GetEventType();

                                    sb.Append(@"EventType/");
                                    sb.Append(element.EventType.GetType().ToString());
                                    sb.Append(eventFieldTerminator);

                                    sb.Append(@"OriginatingRouterName/");
                                    sb.Append(element.OriginatingRouterName.GetType().ToString());
                                    sb.Append(eventFieldTerminator);

                                    sb.Append(@"InRouterName/");
                                    sb.Append(element.InRouterName.GetType().ToString());

                                    foreach (WspKeyValuePair<string, object> kv in serializationData)
                                    {
                                        sb.Append(eventFieldTerminator);
                                        sb.Append(kv.Key);
                                        sb.Append(@"/");
                                        sb.Append(kv.ValueIn.GetType().ToString());
                                    }

                                    sb.Append(eventInfo.RowTerminator);

                                    eventInfo.HeaderRow = sb.ToString();

                                    eventStream.Write(eventInfo.HeaderRow);
                                }

                                eventStream.Write(element.EventType.ToString() + eventFieldTerminator);
                                eventStream.Write(element.OriginatingRouterName + eventFieldTerminator);
                                eventStream.Write(element.InRouterName);

                                serializationData = new SerializationData(element.SerializedEvent);

                                serializationData.GetOriginatingRouterName();
                                serializationData.GetInRouterName();
                                serializationData.GetEventType();

                                foreach (WspKeyValuePair<string, object> kv in serializationData)
                                {
                                    eventStream.Write(eventFieldTerminator);
                                    eventStream.Write(kv.ValueIn.ToString());
                                }

                                eventStream.Write(eventInfo.RowTerminator);

                                if (eventStream.BaseStream.Length >= eventInfo.MaxFileSize)
                                {
                                    eventInfo.NextCopyTick = currentTick - 1;
                                    nextCopyTick = eventInfo.NextCopyTick;
                                }
                            }
                        }
                    }
                }

                catch (IOException e)
                {
                    EventLog.WriteEntry("WspEventRouter", e.ToString(), EventLogEntryType.Error);
                    Thread.Sleep(60000);
                }

                catch (ThreadAbortException e)
                {
                    throw e;
                }

                catch (Exception e)
                {
                    EventLog.WriteEntry("WspEventRouter", e.ToString(), EventLogEntryType.Error);
                    throw e;
                }
            }

            private void CopyFile()
            {
                PersistEventInfo eventInfo;
                bool inCopy = true;

                string[] files;

                FileInfo currentFileInfo;
                FileInfo listFileInfo = null;

                try
                {
                    copyInProcess = true;

                    foreach (Guid eventType in persistEvents.Keys)
                    {
                        eventInfo = persistEvents[eventType];

                        files = Directory.GetFiles(eventInfo.TempFileDirectory);
                        currentFileInfo = new FileInfo(eventInfo.OutFileName);

                        for (int i = 0; i < files.Length; i++)
                        {
                            try
                            {
                                listFileInfo = new FileInfo(files[i]);

                                if (eventInfo.InUse == false || string.Compare(currentFileInfo.Name, listFileInfo.Name, true) != 0)
                                {
                                    try
                                    {
                                        inCopy = true;

                                        File.Copy(files[i], eventInfo.CopyToFileDirectory + @"temp\" + listFileInfo.Name, true);

                                        inCopy = false;

                                        SendPersistEvent(PersistFileState.Copy, eventInfo, files[i]);

                                        try
                                        {
                                            File.Move(eventInfo.CopyToFileDirectory + @"temp\" + listFileInfo.Name, eventInfo.CopyToFileDirectory + listFileInfo.Name);

                                            SendPersistEvent(PersistFileState.Move, eventInfo, eventInfo.CopyToFileDirectory + listFileInfo.Name);
                                        }
                                        catch (IOException)
                                        {
                                            SendPersistEvent(PersistFileState.MoveFailed, eventInfo, eventInfo.CopyToFileDirectory + @"temp\" + listFileInfo.Name);

                                            if (File.Exists(eventInfo.CopyToFileDirectory + listFileInfo.Name) == true &&
                                                new FileInfo(eventInfo.CopyToFileDirectory + listFileInfo.Name).Length ==
                                                new FileInfo(eventInfo.CopyToFileDirectory + @"temp\" + listFileInfo.Name).Length)
                                            {
                                                File.Delete(eventInfo.CopyToFileDirectory + @"temp\" + listFileInfo.Name);
                                            }
                                            else
                                            {
                                                File.Delete(eventInfo.CopyToFileDirectory + listFileInfo.Name);
                                                File.Move(eventInfo.CopyToFileDirectory + @"temp\" + listFileInfo.Name, eventInfo.CopyToFileDirectory + listFileInfo.Name);

                                                SendPersistEvent(PersistFileState.Move, eventInfo, eventInfo.CopyToFileDirectory + listFileInfo.Name);
                                            }
                                        }

                                        File.Delete(files[i]);
                                    }
                                    catch
                                    {
                                        if (inCopy == true)
                                        {
                                            SendPersistEvent(PersistFileState.CopyFailed, eventInfo, files[i]);
                                        }
                                        else
                                        {
                                            SendPersistEvent(PersistFileState.MoveFailed, eventInfo, eventInfo.CopyToFileDirectory + @"temp\" + listFileInfo.Name);
                                        }

                                        Directory.CreateDirectory(eventInfo.CopyToFileDirectory);
                                        Directory.CreateDirectory(eventInfo.CopyToFileDirectory + @"temp\");

                                        inCopy = true;

                                        File.Copy(files[i], eventInfo.CopyToFileDirectory + @"temp\" + listFileInfo.Name, true);

                                        inCopy = false;

                                        SendPersistEvent(PersistFileState.Copy, eventInfo, files[i]);

                                        File.Move(eventInfo.CopyToFileDirectory + @"temp\" + listFileInfo.Name, eventInfo.CopyToFileDirectory + listFileInfo.Name);

                                        SendPersistEvent(PersistFileState.Move, eventInfo, eventInfo.CopyToFileDirectory + listFileInfo.Name);

                                        File.Delete(files[i]);
                                    }
                                }
                            }
                            catch
                            {
                                if (inCopy == true)
                                {
                                    SendPersistEvent(PersistFileState.CopyFailed, eventInfo, files[i]);
                                }
                                else
                                {
                                    if (listFileInfo == null)
                                    {
                                        SendPersistEvent(PersistFileState.MoveFailed, eventInfo, null);
                                    }
                                    else
                                    {
                                        SendPersistEvent(PersistFileState.MoveFailed, eventInfo, eventInfo.CopyToFileDirectory + @"temp\" + listFileInfo.Name);
                                    }
                                }
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

                finally
                {
                    copyInProcess = false;
                }
            }

            private void SendPersistEvent(PersistFileState fileState, PersistEventInfo eventInfo, string outFileName)
            {
                PersistFileEvent persistFileEvent;

                try
                {
                    persistFileEvent = persistFileEvents.Pop();
                }
                catch
                {
                    persistFileEvent = new PersistFileEvent();
                }

                persistFileEvent.PersistEventType = eventInfo.PersistEventType;
                persistFileEvent.FileState = fileState;
                persistFileEvent.FileName = outFileName;
                persistFileEvent.SettingFieldTerminator = eventInfo.FieldTerminator;
                persistFileEvent.SettingLocalOnly = eventInfo.LocalOnly;
                persistFileEvent.SettingMaxCopyInterval = (int)(eventInfo.CopyIntervalTicks / 10000000);
                persistFileEvent.SettingMaxFileSize = eventInfo.MaxFileSize;
                persistFileEvent.SettingRowTerminator = eventInfo.RowTerminator;
                persistFileEvent.FileNameBase = fileNameBase;

                if (fileState == PersistFileState.Open || outFileName == null)
                {
                    persistFileEvent.FileSize = 0;
                }
                else
                {
                    persistFileEvent.FileSize = (new FileInfo(outFileName)).Length;
                }

                for (int i = 0; i < 20; i++)
                {
                    try
                    {
                        pubMgr.Publish(persistFileEvent.Serialize());
                    }
                    catch
                    {
                        break;
                    }
                }

                persistFileEvents.Push(persistFileEvent);
            }
        }
    }
}
