using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Microsoft.WebSolutionsPlatform.Event;
using Microsoft.WebSolutionsPlatform.Event.PubSubManager;

[assembly: CLSCompliant(true)]

namespace WspEventListenTest
{
    class Program
    {
        public static object obj;
        public static int numIterations;
        static DateTime startTime;

        static void Main(/*string[] args*/)
        {
            WorkerClass wc = new WorkerClass();
            TimeSpan totalTime;
            int iterations;

            obj = new object();
            numIterations = 0;

            Thread runThread = new Thread(new ThreadStart(wc.Run));
            runThread.Start();

            while (runThread.ThreadState != ThreadState.Aborted && runThread.ThreadState != ThreadState.Stopped)
            {
                startTime = DateTime.Now;

                Thread.Sleep(1000);

                totalTime = new TimeSpan(DateTime.Now.Ticks - startTime.Ticks);

                lock (obj)
                {
                    iterations = numIterations;
                    numIterations = 0;
                }

                Console.WriteLine((iterations / totalTime.TotalSeconds).ToString() + 
                    " events per second  --  Number of iterations = " + iterations.ToString());
            }

            try
            {
                AppDomain.Unload(AppDomain.CurrentDomain);
            }
            catch
            {
            }
        }
    }

    class WorkerClass : ISubscriptionCallback
    {
        SubscriptionManager subMgr;
        SubscriptionManager.Callback subCallback;

        internal void Run()
        {
            int input;
            char ch;
            WebpageEvent webPageEvent = new WebpageEvent();

            try
            {
                subCallback = new SubscriptionManager.Callback(SubscriptionCallback);

                subMgr = new SubscriptionManager(subCallback);
                subMgr.AddSubscription(webPageEvent.EventType, true);

                while (true)
                {
                    input = Console.Read();

                    ch = Convert.ToChar(input);

                    if (ch == 'q' || ch == 'Q')
                    {
                        break;
                    }
                }
            }
            catch
            {
                //intentionally left blank
            }
            finally
            {
                subMgr.RemoveSubscription(webPageEvent.EventType);

                subMgr.ListenForEvents = false;

                Thread.CurrentThread.Abort();
            }
        }

        public void SubscriptionCallback(Guid eventType, byte[] serializedEvent)
        {
            WebpageEvent localEvent;

            localEvent = new WebpageEvent(serializedEvent);

            lock (Program.obj)
            {
                Program.numIterations++;
            }

            return;
        }
    }
}
