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
        internal class PersistEventInfo
        {
            internal string CopyToFileDirectory;
            internal string TempFileDirectory;
            internal bool LocalOnly;
            internal string OutFileName;
            internal StreamWriter OutStream;
            internal string FieldTerminator;
            internal string RowTerminator;
            internal string HeaderRow;

            internal PersistEventInfo()
            {
                HeaderRow = null;
            }
        }

        internal class Persister : ServiceThread
        {
            internal static bool persistAllEvents;
            internal static bool localOnly = true;

            internal static Dictionary<Guid, PersistEventInfo> persistEvents = new Dictionary<Guid, PersistEventInfo>();

            private int samplingIncrementPerHour; // Number of sampling increments per hour
            private long samplingTicks;
            private long currSampleTicks;
            private long nextSampleTicks;

            private string fileNameBase = string.Empty;
            private string fileNameSuffix = string.Empty;

            private QueueElement element;
            private QueueElement defaultElement = default(QueueElement);

            public Persister()
            {
                fileNameBase = Dns.GetHostName() + @".Events.";
                fileNameSuffix = @".evt";

                samplingIncrementPerHour = 60; // Number of sampling increments per hour
                samplingTicks = CalcTimeInterval(samplingIncrementPerHour);
                currSampleTicks = StartTicks(samplingTicks);
                nextSampleTicks = currSampleTicks + samplingTicks;
            }

            public override void Start()
            {
                bool elementRetrieved;
                SerializationData serializationData;
                PersistEventInfo eventInfo;
                string allFieldTerminator = @",";
                string eventFieldTerminator = @",";

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
                        if (persistEvents.Count == 0)
                        {
                            Thread.Sleep(1000);
                            continue;
                        }
                        else
                        {
                            break;
                        }
                    }

                    while (true)
                    {
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

                        if (DateTime.UtcNow.Ticks > nextSampleTicks)
                        {
                            currSampleTicks = nextSampleTicks;
                            nextSampleTicks = nextSampleTicks + samplingTicks;

                            foreach (Guid eventType in persistEvents.Keys)
                            {
                                eventInfo = persistEvents[eventType];

                                if (eventInfo.OutFileName != null)
                                {
                                    eventInfo.OutStream.Close();
                                }

                                eventInfo.OutFileName = eventInfo.TempFileDirectory + fileNameBase + currSampleTicks.ToString() + fileNameSuffix;

                                if (File.Exists(eventInfo.OutFileName) == true)
                                    eventInfo.OutStream = new StreamWriter(File.Open(eventInfo.OutFileName, FileMode.Append, FileAccess.Write, FileShare.None), Encoding.Unicode);
                                else
                                    eventInfo.OutStream = new StreamWriter(File.Open(eventInfo.OutFileName, FileMode.Create, FileAccess.Write, FileShare.None), Encoding.Unicode);

                                if (eventInfo.HeaderRow != null)
                                {
                                    eventInfo.OutStream.Write(eventInfo.HeaderRow);
                                }
                            }

                            Thread copyThread = new Thread(new ThreadStart(CopyFile));
                            copyThread.Start();
                        }

                        if (elementRetrieved == true)
                        {
                            StreamWriter eventStream = null;
                            StreamWriter allEventStream = null;

                            if (persistAllEvents == true)
                            {
                                allFieldTerminator = persistEvents[Guid.Empty].FieldTerminator;

                                allEventStream = persistEvents[Guid.Empty].OutStream;

                                if (allEventStream == null)
                                {
                                    eventInfo = persistEvents[Guid.Empty];

                                    eventInfo.OutFileName = eventInfo.TempFileDirectory + fileNameBase + currSampleTicks.ToString() + fileNameSuffix;

                                    if (File.Exists(eventInfo.OutFileName) == true)
                                        eventInfo.OutStream = new StreamWriter(File.Open(eventInfo.OutFileName, FileMode.Append, FileAccess.Write, FileShare.None), Encoding.Unicode);
                                    else
                                        eventInfo.OutStream = new StreamWriter(File.Open(eventInfo.OutFileName, FileMode.Create, FileAccess.Write, FileShare.None), Encoding.Unicode);

                                    allEventStream = persistEvents[Guid.Empty].OutStream;
                                }

                                if (persistEvents[Guid.Empty].HeaderRow == null)
                                {
                                    persistEvents[Guid.Empty].HeaderRow = @"$" + persistEvents[Guid.Empty].RowTerminator;
                                    allEventStream.Write(persistEvents[Guid.Empty].HeaderRow);
                                }

                                allEventStream.Write(element.EventType.ToString() + allFieldTerminator);
                                allEventStream.Write(element.OriginatingRouterName + allFieldTerminator);
                                allEventStream.Write(element.InRouterName);
                            }

                            if (persistEvents.TryGetValue(element.EventType, out eventInfo) == true)
                            {
                                eventFieldTerminator = eventInfo.FieldTerminator;

                                eventStream = eventInfo.OutStream;

                                if (eventStream == null)
                                {
                                    eventInfo.OutFileName = eventInfo.TempFileDirectory + fileNameBase + currSampleTicks.ToString() + fileNameSuffix;

                                    if (File.Exists(eventInfo.OutFileName) == true)
                                        eventInfo.OutStream = new StreamWriter(File.Open(eventInfo.OutFileName, FileMode.Append, FileAccess.Write, FileShare.None), Encoding.Unicode);
                                    else
                                        eventInfo.OutStream = new StreamWriter(File.Open(eventInfo.OutFileName, FileMode.Create, FileAccess.Write, FileShare.None), Encoding.Unicode);

                                    eventStream = eventInfo.OutStream;
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
                            }

                            serializationData = new SerializationData(element.SerializedEvent);

                            serializationData.GetOriginatingRouterName();
                            serializationData.GetInRouterName();
                            serializationData.GetEventType();

                            foreach (WspKeyValuePair<string, object> kv in serializationData)
                            {
                                if (allEventStream != null)
                                {
                                    allEventStream.Write(allFieldTerminator);
                                    allEventStream.Write(kv.ValueIn.ToString());
                                }

                                if (eventStream != null)
                                {
                                    eventStream.Write(eventFieldTerminator);
                                    eventStream.Write(kv.ValueIn.ToString());
                                }
                            }

                            if (allEventStream != null)
                            {
                                allEventStream.Write(persistEvents[Guid.Empty].RowTerminator);
                            }

                            if (eventStream != null)
                            {
                                eventStream.Write(eventInfo.RowTerminator);
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

                string[] files;

                FileInfo currentFileInfo;
                FileInfo listFileInfo;

                try
                {
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

                                if (string.Compare(currentFileInfo.Name, listFileInfo.Name, true) != 0)
                                {
                                    try
                                    {
                                        File.Copy(files[i], eventInfo.CopyToFileDirectory + @"temp\" + listFileInfo.Name, true);

                                        try
                                        {
                                            File.Move(eventInfo.CopyToFileDirectory + @"temp\" + listFileInfo.Name, eventInfo.CopyToFileDirectory + listFileInfo.Name);
                                        }
                                        catch (IOException)
                                        {
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

                                            }
                                        }

                                        File.Delete(files[i]);
                                    }
                                    catch
                                    {
                                        Directory.CreateDirectory(eventInfo.CopyToFileDirectory);
                                        Directory.CreateDirectory(eventInfo.CopyToFileDirectory + @"temp\");
                                        File.Copy(files[i], eventInfo.CopyToFileDirectory + @"temp\" + listFileInfo.Name, true);
                                        File.Move(eventInfo.CopyToFileDirectory + @"temp\" + listFileInfo.Name, eventInfo.CopyToFileDirectory + listFileInfo.Name);
                                        File.Delete(files[i]);
                                    }
                                }
                            }
                            catch
                            {
                                // left blank to try all files
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

            private static long CalcTimeInterval(int samplingIncrementPerHour)
            {
                DateTime timeHourOne = new DateTime(2005, 1, 1, 1, 0, 0);
                DateTime timeHourTwo = new DateTime(2005, 1, 1, 2, 0, 0);

                long hourTicks = timeHourTwo.Ticks - timeHourOne.Ticks;

                return hourTicks / (long)samplingIncrementPerHour;
            }

            private static long StartTicks(long samplingTicks)
            {
                DateTime timeNow = new DateTime(DateTime.UtcNow.Ticks);
                DateTime timeStart = new DateTime(timeNow.Year, timeNow.Month, timeNow.Day, timeNow.Hour, 0, 0);

                return timeStart.Ticks + (((timeNow.Ticks - timeStart.Ticks) / samplingTicks) * samplingTicks);
            }
        }
    }
}
