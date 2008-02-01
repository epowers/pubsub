using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Web;
using System.Threading;
using Microsoft.WebSolutionsPlatform.Event.PubSubManager;
using Microsoft.WebSolutionsPlatform.Event;

[assembly: CLSCompliant(true)]

namespace WspEventServiceTest
{
	class Program
	{
		static int numIterations;
		static DateTime startTime;
		static List<Thread> workerThreads = new List<Thread>();

		static void Main( string[] args )
		{
			int numThreads;
			bool processingComplete = false;

			startTime = DateTime.Now;

			if(args.Length == 0)
			{
				numIterations = 1000;
				numThreads = 1;
			}
			else
			{
				numIterations = Convert.ToInt32(args[0]);

				if(args.Length == 1)
				{
					numThreads = 1;
				}
				else
				{
					numThreads = Convert.ToInt32(args[1]);
				}
			}

			for(int i = 0; i < numThreads; i++)
			{
				WorkerClass wc = new WorkerClass();

				Thread t = new Thread(new ThreadStart(wc.SendEvents));
				t.Name = i.ToString();
				workerThreads.Add(t);
			}

			for(int i = 0; i < numThreads; i++)
			{
				workerThreads[i].Start();
			}

			while(processingComplete == false)
			{
				Thread.Sleep(1000);

				processingComplete = true;

				for(int i = 0; i < numThreads; i++)
				{
					if(workerThreads[i].IsAlive == true)
						processingComplete = false;
				}
			}

			DateTime stopTime = DateTime.Now;
			TimeSpan totalTime = new TimeSpan(stopTime.Ticks - startTime.Ticks);

			Console.WriteLine(" ");

			Console.WriteLine("Overall time was: " + totalTime.TotalSeconds.ToString() + " seconds");
			Console.WriteLine(((numIterations * numThreads) / totalTime.TotalSeconds).ToString() + " events per second");
		}

		class WorkerClass
		{
            private PublishManager pubMgr;

			public void SendEvents()
			{
                try
                {
                    pubMgr = new PublishManager();
                }
                catch(Exception e)
                {
                    Console.WriteLine(e.Message);
                    return;
                }

                WebpageEvent localEvent = new WebpageEvent();
				localEvent.EventName = @"Test Event";

				localEvent.ActiveXControls = true;
				localEvent.AnonymousId = Guid.NewGuid();
				localEvent.Aol = true;
				localEvent.BackgroundSounds = true;
				localEvent.Beta = false;
				localEvent.Browser = @"IE 6.0";
				localEvent.Cdf = false;
				localEvent.ClrVersion = new Version(@"1.2.3.4");
				localEvent.Cookies = true;
				localEvent.Crawler = false;
				localEvent.EcmaScriptVersion = new Version(@"1.2.3.4");
				localEvent.Ext = string.Empty;
				localEvent.Frames = true;
				localEvent.HostDomain = @"microsoft";
				localEvent.JavaApplets = true;
				localEvent.LogicalUri = new Uri(@"http://www.microsoft.com");
				localEvent.MajorVersion = 1;
				localEvent.MinorVersion = 2.3;
				localEvent.MSDomVersion = new Version(@"1.2.3.4");
				localEvent.Platform = @"Windows XP";
				localEvent.RequestType = @"Page Request";
				localEvent.Source = @"source";
				localEvent.SourceServer = @"sourceserver";
				localEvent.StatusCode = 0;
				localEvent.SubDirectory = @"eventcollection";
				localEvent.Tables = true;
				localEvent.Type = @"type";
				localEvent.UriHash = new System.Security.Cryptography.MD5CryptoServiceProvider();
				localEvent.UriHash.Initialize();
				localEvent.UriQuery = new Uri(@"http://www.microsoft.com/test");
				localEvent.UriStem = new Uri(@"http://www.microsoft.com");
				localEvent.UrlReferrer = new Uri(@"http://www.microsoft.com");
				localEvent.UrlReferrerDomain = @"microsoft.com";
				localEvent.UserAgent = @"useragent kasgfig;lkartpoiwhtlnvoaing;oakng;aih;akng;lna;kn";
				localEvent.UserHostAddress = new IPAddress(0x2414188f);
				localEvent.VBScript = true;
				localEvent.Version = @"1.2.3.4";
				localEvent.VirtualRoot = @"http://www.microsoft.com/test";
				localEvent.W3CDomVersion = new Version(@"1.2.3.4");
				localEvent.Win16 = false;
				localEvent.Win32 = true;

                try
                {
                    for (int i = 0; i < numIterations; i++)
                    {
                        try
                        {
                            pubMgr.Publish(localEvent.Serialize());
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine("Item: " + i.ToString() + @"    Exception: " + e.Message);
                            i--;
                        }
                    }
                }

                catch (Exception e)
                {
                    Console.WriteLine(@"Exception: " + e.Message);
                }

                DateTime stopTime = DateTime.Now;
                TimeSpan totalTime = new TimeSpan(stopTime.Ticks - startTime.Ticks);

                Console.WriteLine("Total time was: " + totalTime.TotalSeconds.ToString() + " seconds");
                Console.WriteLine((numIterations / totalTime.TotalSeconds).ToString() + " events per second");

                localEvent.UriHash.Clear();
                localEvent.UriHash = null;
            }
		}
	}
}
