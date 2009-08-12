using System;
using System.Collections.Generic;
using System.Windows.Forms;
using Microsoft.WebSolutionsPlatform.Event;

[assembly: CLSCompliant(true)]

namespace Microsoft.Sample.EventPingPong
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Form1());
        }
    }

    public class PublishEvent : Event
    {
        private UInt64 eventNum;
        [CLSCompliant(false)]
        public UInt64 EventNum
        {
            get
            {
                return eventNum;
            }

            set
            {
                eventNum = value;
            }
        }

        private Guid instanceId;
        public Guid InstanceId
        {
            get
            {
                return instanceId;
            }

            set
            {
                instanceId = value;
            }
        }

		/// <summary>
		/// Base constructor to create a new event
		/// </summary>
        public PublishEvent()
            : base()
		{
            EventType = new Guid(@"24923A0F-04CB-4dcb-8EA9-3D1CAA316B23");
		}

        /// <summary>
        /// Base constructor to create a new event from a serialized event
        /// </summary>
        /// <param name="serializationData">Serialized event buffer</param>
        public PublishEvent(byte[] serializationData)
            : base(serializationData)
        {
            EventType = new Guid(@"24923A0F-04CB-4dcb-8EA9-3D1CAA316B23");
        }
  
		/// <summary>
		/// Used for event serialization.
		/// </summary>
		/// <param name="buffer">SerializationData object passed to store serialized object</param>
        public override void GetObjectData(WspBuffer buffer)
        {
            buffer.AddElement(@"EventNum", EventNum);
            buffer.AddElement(@"InstanceId", InstanceId);
        }

        /// <summary>
        /// Set values on object during deserialization
        /// </summary>
        /// <param name="elementName">Name of property</param>
        /// <param name="elementValue">Value of property</param>
        /// <returns></returns>
        public override bool SetElement(string elementName, object elementValue)
        {
            switch (elementName)
            {
                case "EventNum":
                    EventNum = (UInt64)elementValue;
                    break;

                case "InstanceId":
                    InstanceId = (Guid)elementValue;
                    break;

                default:
                    base.SetElement(elementName, elementValue);
                    break;
            }

            return true;
        }
    }

    public class SubscribeEvent : Event
    {
        private UInt64 eventNum;
        [CLSCompliant(false)]
        public UInt64 EventNum
        {
            get
            {
                return eventNum;
            }

            set
            {
                eventNum = value;
            }
        }

        private Guid instanceId;
        public Guid InstanceId
        {
            get
            {
                return instanceId;
            }

            set
            {
                instanceId = value;
            }
        }

        /// <summary>
        /// Base constructor to create a new event
        /// </summary>
        public SubscribeEvent()
            : base()
        {
            EventType = new Guid(@"AF211692-E5A4-471d-8FAA-21F0D18EA60B");
        }

        /// <summary>
        /// Base constructor to create a new event from a serialized event
        /// </summary>
        /// <param name="serializationData">Serialized event buffer</param>
        public SubscribeEvent(byte[] serializationData)
            : base(serializationData)
        {
            EventType = new Guid(@"AF211692-E5A4-471d-8FAA-21F0D18EA60B");
        }

        /// <summary>
        /// Used for event serialization.
        /// </summary>
        /// <param name="buffer">SerializationData object passed to store serialized object</param>
        public override void GetObjectData(WspBuffer buffer)
        {
            buffer.AddElement(@"EventNum", EventNum);
            buffer.AddElement(@"InstanceId", InstanceId);
        }

        /// <summary>
        /// Set values on object during deserialization
        /// </summary>
        /// <param name="elementName">Name of property</param>
        /// <param name="elementValue">Value of property</param>
        /// <returns></returns>
        public override bool SetElement(string elementName, object elementValue)
        {
            switch(elementName)
            {
                case "EventNum":
                    EventNum = (UInt64)elementValue;
                    break;

                case "InstanceId":
                    InstanceId = (Guid)elementValue;
                    break;

                default:
                    base.SetElement(elementName, elementValue);
                    break;
            }

            return true;
        }
    }
}