using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Diagnostics;
using System.Threading;
using System.Windows.Forms;
using Microsoft.WebSolutionsPlatform.PubSubManager;

namespace Microsoft.Sample.EventPingPong
{
    public partial class Form1 : Form, IObserver<WspEvent>
    {
        private static object obj = new object();
        private static Guid instanceId = Guid.NewGuid();
        private static UInt64 counter;
        private static DateTime startTime;
        private static TimeSpan totalTime;
        private static PublishEvent pubEvent;
        private static SubscribeEvent subEvent;
        private static WspEventPublish eventPush;

        WspEventObservable subObservable;
        IDisposable subObservableDispose = null;
        Guid subEventType;

        WspEventObservable pubObservable;
        IDisposable pubObservableDispose = null;
        Guid pubEventType;

        delegate void SetTextCallback(TextBox tb, string text);

        public Form1()
        {
            InitializeComponent();

            pubEvent = new PublishEvent();
            pubEvent.InstanceId = instanceId;

            subEvent = new SubscribeEvent();

            eventPush = new WspEventPublish();

            stopPubSubButton.Checked = true;
            stopPubSubButton.Select();
        }

        private void stopPubSubButton_CheckedChanged(object sender, EventArgs e)
        {
            if (stopPubSubButton.Checked == true)
            {
                publishEventsButton.Checked = false;
                subscribeEventsButton.Checked = false;

                StopPubSub();
            }
        }

        private void StopPubSub()
        {
            StopSubscribe();
            StopPublish();
        }

        public void StartPublish()
        {
            startTime = DateTime.Now;
            counter = 0;

            timer1.Start();

            pubEvent.EventNum = 0;

            pubEventType = pubEvent.InstanceId;
            pubObservable = new WspEventObservable(pubEventType, false);
            pubObservableDispose = pubObservable.Subscribe(this);

            while (true)
            {
                try
                {
                    eventPush.OnNext(new WspEvent(pubEvent.EventType, null, pubEvent.Serialize()));

                    break;
                }
                catch
                {
                    Thread.Sleep(2);
                }
            }
        }

        public void StopPublish()
        {
            try
            {
                timer1.Stop();

                if (pubObservableDispose != null)
                {
                    pubObservableDispose.Dispose();
                    pubObservableDispose = null;
                }
            }
            catch
            {
            }
        }

        public void StartSubscribe()
        {
            startTime = DateTime.Now;
            counter = 0;

            timer1.Start();

            subEventType = pubEvent.EventType;
            subObservable = new WspEventObservable(subEventType, false);
            subObservableDispose = subObservable.Subscribe(this);
        }

        public void StopSubscribe()
        {
            try
            {
                timer1.Stop();

                if (subObservableDispose != null)
                {
                    subObservableDispose.Dispose();
                    subObservableDispose = null;
                }
            }
            catch
            {
            }
        }

        private void publishEventsButton_CheckedChanged(object sender, EventArgs e)
        {
            if (publishEventsButton.Checked == true)
            {
                stopPubSubButton.Checked = false;
                subscribeEventsButton.Checked = false;

                StopPubSub();
                StartPublish();
            }
        }

        private void subscribeEventsButton_CheckedChanged(object sender, EventArgs e)
        {
            if (subscribeEventsButton.Checked == true)
            {
                stopPubSubButton.Checked = false;
                publishEventsButton.Checked = false;

                StopPubSub();
                StartSubscribe();
            }
        }

        private void quitButton_Click(object sender, EventArgs e)
        {
            StopPubSub();

            Process.GetCurrentProcess().Kill();
        }

        private void SetTextbox(TextBox tb, string text)
        {
            try
            {
                if (this.InvokeRequired)
                {
                    SetTextCallback d = new SetTextCallback(SetTextbox);

                    this.Invoke(d, new object[] { tb, text });
                }
                else
                {
                    tb.Text = text;
                }
            }
            catch (ObjectDisposedException)
            {
                // This may happen when you Quit
            }
        }

        public void OnNext(WspEvent wspEvent)
        {
            if (wspEvent.EventType == pubEventType)
            {
                PublishCallback(wspEvent.EventType, wspEvent);
            }
            else
            {
                SubscriptionCallback(wspEvent.EventType, wspEvent);
            }

            return;
        }

        public void OnCompleted()
        {
        }

        public void OnError(Exception e)
        {
        }


        private void SubscriptionCallback(Guid eventType, WspEvent wspEvent)
        {
            PublishEvent localEvent;

            localEvent = new PublishEvent(wspEvent.Body);

            Thread.Sleep(0);

            lock (obj)
            {
                counter++;

                subEvent.EventNum = localEvent.EventNum;
                subEvent.InstanceId = localEvent.InstanceId;
                subEvent.EventType = localEvent.InstanceId;

                while (true)
                {
                    try
                    {
                        eventPush.OnNext(new WspEvent(subEvent.EventType, null, subEvent.Serialize()));

                        break;
                    }
                    catch
                    {
                        Thread.Sleep(2);
                    }
                }
            }

            SetTextbox(eventNumberSent, counter.ToString());
        }

        private void PublishCallback(Guid eventType, WspEvent wspEvent)
        {
            SubscribeEvent subscribeEvent;

            subscribeEvent = new SubscribeEvent(wspEvent.Body);

            Thread.Sleep(0);

            if (subscribeEvent.InstanceId == instanceId)
            {
                lock (obj)
                {
                    counter++;

                    pubEvent.EventNum++;

                    while (true)
                    {
                        try
                        {
                            eventPush.OnNext(new WspEvent(pubEvent.EventType, null, pubEvent.Serialize()));

                            break;
                        }
                        catch
                        {
                            Thread.Sleep(2);
                        }
                    }
                }

                SetTextbox(eventNumberSent, counter.ToString());
            }
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            totalTime = new TimeSpan(DateTime.Now.Ticks - startTime.Ticks);

            SetTextbox(roundtripRate, ((double)(counter / totalTime.TotalSeconds)).ToString());
        }
    }
}