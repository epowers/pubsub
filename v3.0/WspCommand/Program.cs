using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Reflection;
using Microsoft.WebSolutionsPlatform.Event;
using Microsoft.WebSolutionsPlatform.PubSubManager;

namespace WspCommand
{
    class Program
    {
        static object lockObj = new object();

        static void Main(string[] args)
        {
            bool showHelp = false;
            char[] splitChar = { '=' };

            CommandRequest commandRequest = new CommandRequest();
            commandRequest.EventType = new Guid("C8EDEB22-7E4A-4441-B7B4-419DDB856321");
            commandRequest.EventIdForResponse = Guid.NewGuid();
            commandRequest.CorrelationID = Guid.NewGuid();
            commandRequest.Command = string.Empty;

            if (args.Length == 0)
            {
                showHelp = true;
            }
            else
            {
                commandRequest.Command = args[0].ToLower();

                for (int i = 1; i < args.Length; i++)
                {
                    if (args[i].StartsWith("/") == true)
                    {
                        string option = args[i].Split(splitChar)[0].ToLower();

                        switch (option)
                        {
                            case "/eventtype":
                                try
                                {
                                    commandRequest.EventType = new Guid(args[i].Split(splitChar)[1]);
                                }
                                catch
                                {
                                    ConsoleColor currColor = Console.ForegroundColor;
                                    Console.ForegroundColor = ConsoleColor.Red;

                                    Console.WriteLine("ERROR: Bad 'EventType' parameter");

                                    Console.ForegroundColor = currColor;

                                    return;
                                }

                                break;

                            case "/target":
                                try
                                {
                                    commandRequest.TargetMachineFilter = args[i].Split(splitChar)[1];
                                }
                                catch
                                {
                                    ConsoleColor currColor = Console.ForegroundColor;
                                    Console.ForegroundColor = ConsoleColor.Red;

                                    Console.WriteLine("ERROR: Bad 'Target' parameter");

                                    Console.ForegroundColor = currColor;

                                    return;
                                }

                                break;

                            case "/role":
                                try
                                {
                                    commandRequest.TargetRoleFilter = args[i].Split(splitChar)[1];
                                }
                                catch
                                {
                                    ConsoleColor currColor = Console.ForegroundColor;
                                    Console.ForegroundColor = ConsoleColor.Red;

                                    Console.WriteLine("ERROR: Bad 'Role' parameter");

                                    Console.ForegroundColor = currColor;

                                    return;
                                }

                                break;

                            default:
                                showHelp = true;

                                break;
                        }
                    }
                    else
                    {
                        commandRequest.Arguments.Add(args[i]);
                    }
                }
            }

            SubscriptionManager responseMgr;
            SubscriptionManager.Callback responseCallback;

            responseCallback = new SubscriptionManager.Callback(ResponseCallback);

            try
            {
                responseMgr = new SubscriptionManager(responseCallback);
            }
            catch (PubSubQueueDoesNotExistException)
            {
                ConsoleColor currColor = Console.ForegroundColor;
                Console.ForegroundColor = ConsoleColor.Red;

                Console.WriteLine("Error: the WspEventRouter service is not running");

                Console.ForegroundColor = currColor;

                return;
            }

            responseMgr.AddSubscription(commandRequest.EventIdForResponse, false);

            PublishManager pubMgr = new PublishManager();

            switch (commandRequest.Command)
            {
                case "wsp_processcommands":
                    if (commandRequest.Arguments.Count == 0)
                    {
                        commandRequest.Arguments.Add("true");
                    }
                    else
                    {
                        if (commandRequest.Arguments.Count > 1 ||
                            (string.Compare((string)commandRequest.Arguments[0], "true", true) != 0 &&
                            string.Compare((string)commandRequest.Arguments[0], "false", true) != 0))
                        {
                            ConsoleColor currColor = Console.ForegroundColor;
                            Console.ForegroundColor = ConsoleColor.Red;

                            Console.WriteLine("ERROR: Invalid arguments");

                            Console.ForegroundColor = currColor;

                            responseMgr.ListenForEvents = false;

                            return;
                        }
                    }

                    break;

                case "wsp_getdeviceinfo":
                case "wsp_getprocessinfo":
                case "wsp_getserviceinfo":
                case "wsp_getnetworkinfo":
                case "wsp_getdriveinfo":
                case "wsp_geteventloginfo":
                case "wsp_getwspassemblyinfo":
                case "wsp_getsysteminfo":
                    if (commandRequest.Arguments.Count > 0)
                    {
                        ConsoleColor currColor = Console.ForegroundColor;
                        Console.ForegroundColor = ConsoleColor.Red;

                        Console.WriteLine("ERROR: Invalid arguments");

                        Console.ForegroundColor = currColor;

                        responseMgr.ListenForEvents = false;

                        return;
                    }

                    break;

                case "wsp_getperformancecounters":
                case "wsp_getfileversioninfo":
                case "wsp_getregistrykeys":
                    if (commandRequest.Arguments.Count == 0)
                    {
                        ConsoleColor currColor = Console.ForegroundColor;
                        Console.ForegroundColor = ConsoleColor.Red;

                        Console.WriteLine("ERROR: Argument list is empty");

                        Console.ForegroundColor = currColor;

                        responseMgr.ListenForEvents = false;

                        return;
                    }

                    break;

                default:
                    showHelp = true;
                    break;
            }

            if (showHelp == true)
            {
                ConsoleColor currColor = Console.ForegroundColor;
                Console.ForegroundColor = ConsoleColor.Green;

                Console.Write("\nCommand Format:\n\n");
                Console.Write("\tWspCommand <cmd> [argsIn] [/target=<regex filter>] [/role=<Wsp Role>] [/eventtype=<Guid>]\n\n\n");
                Console.Write("\tCommands:\n\n");
                Console.Write("\t\tWsp_GetDeviceInfo\n");
                Console.Write("\t\tWsp_GetDriveInfo\n");
                Console.Write("\t\tWsp_GetEventLogInfo\n");
                Console.Write("\t\tWsp_GetFileVersionInfo <argsIn>\n");
                Console.Write("\t\tWsp_GetNetworkInfo\n");
                Console.Write("\t\tWsp_GetPerformanceCounters <argsIn>\n");
                Console.Write("\t\tWsp_GetProcessInfo\n");
                Console.Write("\t\tWsp_GetRegistryKeys <argsIn>\n");
                Console.Write("\t\tWsp_GetServiceInfo\n");
                Console.Write("\t\tWsp_GetSystemInfo\n");
                Console.Write("\t\tWsp_GetWspAssemblyInfo\n");

                Console.ForegroundColor = currColor;

                responseMgr.ListenForEvents = false;

                return;
            }

            pubMgr.Publish(commandRequest.EventType, commandRequest.Serialize());

            Console.WriteLine();

            ConsoleColor c = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Red;

            Console.WriteLine("---- Press any key to end ----");

            Console.ForegroundColor = c;

            Console.ReadKey();

            responseMgr.ListenForEvents = false;

            return;
        }

        static public void ResponseCallback(Guid eventType, Microsoft.WebSolutionsPlatform.PubSubManager.WspEvent wspEvent)
        {
            CommandResponse response;

            response = new CommandResponse(wspEvent.Body);

            lock (lockObj)
            {
                ConsoleColor c = Console.ForegroundColor;
                Console.ForegroundColor = ConsoleColor.Green;

                Console.WriteLine();

                Console.WriteLine("OriginatingRouterName: " + response.OriginatingRouterName);
                Console.WriteLine("ReturnCode: " + response.ReturnCode.ToString());
                Console.WriteLine("Message: " + response.Message);
                Console.WriteLine("CorrelationID: " + response.CorrelationID.ToString());

                if (response.ResponseException != null)
                {
                    Console.WriteLine("ResponseException: " + response.ResponseException.Message);
                }

                WriteOutput(response.Results, "Results", 0);

                Console.WriteLine();
                Console.WriteLine();

                Console.ForegroundColor = ConsoleColor.Red;

                Console.WriteLine("---- Press any key to end ----");

                Console.ForegroundColor = c;

                Console.WriteLine();
            }

            return;
        }

        static public void WriteOutput(Dictionary<string, object> dictionary, string name, int level)
        {
            for (int i = 0; i < level; i++)
            {
                Console.Write('\t');
            }

            Console.Write(name + ": \n");

            foreach (string key in dictionary.Keys)
            {
                Type type = dictionary[key].GetType();

                if (type == typeof(Dictionary<string, object>))
                {
                    WriteOutput((Dictionary<string, object>)dictionary[key], key, level + 1);
                }
                else
                {
                    if (type == typeof(List<string>))
                    {
                        WriteOutput((List<string>)dictionary[key], key, level + 1);
                    }
                    else
                    {
                        if (type == typeof(List<object>))
                        {
                            WriteOutput((List<object>)dictionary[key], key, level + 1);
                        }
                        else
                        {
                            WriteOutput((object)dictionary[key], key, level + 1);
                        }
                    }
                }

            }
        }

        static public void WriteOutput(List<string> list, string name, int level)
        {
            for (int i = 0; i < level; i++)
            {
                Console.Write('\t');
            }

            Console.Write(name + ": \n");

            for (int i = 0; i < list.Count; i++)
            {
                WriteOutput((object)list[i], i.ToString(), level + 1);
            }
        }

        static public void WriteOutput(List<object> list, string name, int level)
        {
            for (int i = 0; i < level; i++)
            {
                Console.Write('\t');
            }

            Console.Write(name + ": \n");

            for (int i = 0; i < list.Count; i++)
            {
                object obj = list[i];

                Type type = obj.GetType();

                if (type == typeof(Dictionary<string, object>))
                {
                    WriteOutput((Dictionary<string, object>)obj, i.ToString(), level + 1);
                }
                else
                {
                    if (type == typeof(List<string>))
                    {
                        WriteOutput((List<string>)obj, i.ToString(), level + 1);
                    }
                    else
                    {
                        if (type == typeof(List<object>))
                        {
                            WriteOutput((List<object>)obj, i.ToString(), level + 1);
                        }
                        else
                        {
                            WriteOutput((object)obj, string.Empty, level + 1);
                        }
                    }
                }
            }
        }

        static public void WriteOutput(object obj, string name, int level)
        {
            Type t = obj.GetType();

            for (int i = 0; i < level; i++)
            {
                Console.Write('\t');
            }

            if (name != string.Empty)
            {
                Console.Write(name + ": ");
            }

            if (t == typeof(byte[]))
            {
                Console.Write(BitConverter.ToString((byte[])obj));
                Console.Write("\n");
            }
            else
            {
                Console.Write(obj.ToString() + "\n");
            }
        }
    }
}
