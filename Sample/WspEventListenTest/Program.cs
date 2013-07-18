using System;
using System.Linq.Expressions;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Security.Cryptography;
using Microsoft.WebSolutionsPlatform.Event;
using Microsoft.WebSolutionsPlatform.PubSubManager;

namespace WspEventListenTest
{
    class Program
    {
        public static object obj;
        public static int numIterations;
        public static string filterPattern = string.Empty;
        static DateTime startTime;

        static void Main(string[] args)
        {
            WorkerClass wc = new WorkerClass();
            TimeSpan totalTime;
            int iterations;

            obj = new object();
            numIterations = 0;

            if (args.Length > 0)
            {
                filterPattern = args[0];
            }

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

    class WorkerClass : IObserver<WspEvent>
    {
        WspEventObservable sub;
        MD5 md5Hasher = MD5.Create();

        internal void Run()
        {
            int input;
            char ch;
            WebpageEvent webPageEvent = new WebpageEvent();
            IDisposable eventDispose = null;

            try
            {
                sub = new WspEventObservable(webPageEvent.EventType, false, Program.filterPattern, null, null);

                eventDispose = sub.Subscribe(this);

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
                if (eventDispose != null)
                {
                    eventDispose.Dispose();
                }

                Thread.CurrentThread.Abort();
            }
        }

        public void OnNext(WspEvent wspEvent)
        {
            WebpageEvent localEvent;
            byte[] headerHash;
            byte[] bodyHash;

            if (wspEvent.Headers.ContainsKey(101))
            {
                headerHash = Convert.FromBase64String(wspEvent.Headers[101]);
                bodyHash = md5Hasher.ComputeHash(wspEvent.Body);

                for (int i = 0; i < headerHash.Length; i++)
                {
                    if (headerHash[i] != bodyHash[i])
                    {
                        Console.WriteLine("Error: Hash codes don't match");

                        break;
                    }
                }
            }

            localEvent = new WebpageEvent(wspEvent.Body);

            lock (Program.obj)
            {
                Program.numIterations++;
            }

            return;
        }

        public void OnCompleted()
        {
        }

        public void OnError(Exception e)
        {
        }
    }
}
