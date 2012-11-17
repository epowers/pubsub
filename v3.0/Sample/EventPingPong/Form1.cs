using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using Microsoft.WebSolutionsPlatform.PubSubManager;

namespace Microsoft.Sample.EventPingPong
{
    public partial class Form1 : Form
    {
        private static object obj = new object();
        private static Guid instanceId = Guid.NewGuid();
        private static UInt64 counter;
        private static DateTime startTime;
        private static TimeSpan totalTime;
        private static PublishEvent pubEvent;
        private static SubscribeEvent subEvent;
        private static PublishManager pubMgr;
        private static SubscriptionManager subMgr;
        private static SubscriptionManager.Callback subCallback;
        private static SubscriptionManager.Callback pubCallback;

        delegate void SetTextCallback(TextBox tb, string text);

        public Form1()
        {
            InitializeComponent();

            pubEvent = new PublishEvent();
            pubEvent.InstanceId = instanceId;

            subEvent = new SubscribeEvent();

            pubMgr = new PublishManager();

            subCallback = new SubscriptionManager.Callback(SubscriptionCallback);
            pubCallback = new SubscriptionManager.Callback(PublishCallback);

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

            subMgr = new SubscriptionManager(1, pubCallback);
            subMgr.ListenForEvents = true;
            subMgr.AddSubscription(pubEvent.InstanceId, false);

            while (true)
            {
                try
                {
                    pubMgr.Publish(pubEvent.EventType, pubEvent.Serialize());

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

                if (subMgr != null)
                {
                    subMgr.RemoveSubscription(pubEvent.InstanceId);
                    subMgr.ListenForEvents = false;
                    subMgr = null;
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

            subMgr = new SubscriptionManager(1, subCallback);
            subMgr.ListenForEvents = true;
            subMgr.AddSubscription(pubEvent.EventType, false);
        }

        public void StopSubscribe()
        {
            try
            {
                timer1.Stop();

                if (subMgr != null)
                {
                    subMgr.RemoveSubscription(pubEvent.EventType);
                    subMgr.ListenForEvents = false;
                    subMgr = null;
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

            Application.Exit();
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



        public void SubscriptionCallback(Guid eventType, Microsoft.WebSolutionsPlatform.PubSubManager.WspEvent wspEvent)
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
                        pubMgr.Publish(subEvent.EventType, subEvent.Serialize());

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

        public void PublishCallback(Guid eventType, Microsoft.WebSolutionsPlatform.PubSubManager.WspEvent wspEvent)
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
                            pubMgr.Publish(pubEvent.EventType, pubEvent.Serialize());

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