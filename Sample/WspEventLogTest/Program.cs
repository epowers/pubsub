using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using Microsoft.WebSolutionsPlatform.Event;
using Microsoft.WebSolutionsPlatform.PubSubManager;

[assembly: CLSCompliant(true)]

namespace WspEventLogTest
{
    public class LogEntry : Event
    {
        public bool BooleanProp { get; set; }
        public byte ByteProp { get; set; }
        public byte[] ByteArrayProp { get; set; }
        public char CharProp { get; set; }
        public char[] CharArrayProp { get; set; }
        public decimal DecimalProp { get; set; }
        public double DoubleProp { get; set; }
        public Int16 Int16Prop { get; set; }
        public Int32 Int32Prop { get; set; }
        public Int64 Int64Prop { get; set; }
        [property: CLSCompliant(false)]
        public SByte SByteProp { get; set; }
        public Single SingleProp { get; set; }
        public String StringProp { get; set; }
        [property: CLSCompliant(false)]
        public UInt16 UInt16Prop { get; set; }
        [property: CLSCompliant(false)]
        public UInt32 UInt32Prop { get; set; }
        [property: CLSCompliant(false)]
        public UInt64 UInt64Prop { get; set; }
        public Version VersionProp { get; set; }
        public DateTime DateTimeProp { get; set; }
        public Guid GuidProp { get; set; }
        public Uri UriProp { get; set; }
        private static byte[] ipaddr = {192, 168, 0, 1};
        public IPAddress IPAddressProp { get; set; }
        public Dictionary<string, string> StringDictionaryProp { get; set; }
        public Dictionary<string, object> ObjectDictionaryProp { get; set; }
        public List<string> StringListProp { get; set; }
        public List<object> ObjectListProp { get; set; }

        public LogEntry(byte[] serializedEvent)
            : base(serializedEvent)
        {
        }

        public LogEntry()
            : base()
        {
            this.EventType = new Guid("BAAA4109-BB83-46dc-AFA3-422527F209C7");

            BooleanProp = true;
            ByteProp = 7;
            ByteArrayProp = new byte[] { 1, 2, 3, 4, 5 };
            CharProp = 'a';
            CharArrayProp = new char[] { 'a', 'b', 'c', 'd' };
            DecimalProp = 123;
            DoubleProp = 123.456;
            Int16Prop = -128;
            Int32Prop = -256;
            Int64Prop = -512;
            SByteProp = -8;
            SingleProp = Single.MinValue;
            StringProp = "stringprop value \" \\ / \b \f \n \r \t \\u1234 all done";
            UInt16Prop = 128;
            UInt32Prop = 256;
            UInt64Prop = 512;
            VersionProp = new Version("2.1.2.3");
            DateTimeProp = DateTime.Now;
            GuidProp = Guid.NewGuid();
            UriProp = new Uri("http://localhost/default.htm");
            IPAddressProp = new IPAddress(ipaddr);
            StringDictionaryProp = new Dictionary<string, string>();
            ObjectDictionaryProp = new Dictionary<string, object>();
            StringListProp = new List<string>();
            ObjectListProp = new List<object>();

            StringDictionaryProp["DPkey1"] = "value 1";
            StringDictionaryProp["DPkey2"] = "key2 value \" \\ / \b \f \n \r \t \\u1234 all done";
            StringDictionaryProp["DPkey3 \" \\ / \b \f \n \r \t \\u1234 all done"] = "key3 value";

            StringListProp.Add("string 1");
            StringListProp.Add("string 2 \" \\ / \b \f \n \r \t \\u1234 all done");
            StringListProp.Add("string 3");

            ObjectDictionaryProp["KVkey1"] = StringProp;
            ObjectDictionaryProp["KVkey2"] = DateTimeProp;
            ObjectDictionaryProp["KVkey3"] = GuidProp;
            ObjectDictionaryProp["KVkey4"] = StringDictionaryProp;
            ObjectDictionaryProp["KVkey5"] = StringListProp;
            ObjectDictionaryProp["KVkey6"] = Int32Prop;

            ObjectListProp.Add(ObjectDictionaryProp);
        }

        public override void GetObjectData(WspBuffer buffer)
        {
            buffer.AddElement("BooleanProp", BooleanProp);
            buffer.AddElement("ByteProp", ByteProp);
            buffer.AddElement("ByteArrayProp", ByteArrayProp);
            buffer.AddElement("CharProp", CharProp);
            buffer.AddElement("CharArrayProp", CharArrayProp);
            buffer.AddElement("StringListProp", StringListProp);
            buffer.AddElement("ObjectListProp", ObjectListProp);
            buffer.AddElement("DecimalProp", DecimalProp);
            buffer.AddElement("DoubleProp", DoubleProp);
            buffer.AddElement("Int16Prop", Int16Prop);
            buffer.AddElement("Int32Prop", Int32Prop);
            buffer.AddElement("Int64Prop", Int64Prop);
            buffer.AddElement("SByteProp", SByteProp);
            buffer.AddElement("SingleProp", SingleProp);
            buffer.AddElement("StringProp", StringProp);
            buffer.AddElement("StringDictionaryProp", StringDictionaryProp);
            buffer.AddElement("UInt16Prop", UInt16Prop);
            buffer.AddElement("UInt32Prop", UInt32Prop);
            buffer.AddElement("UInt64Prop", UInt64Prop);
            buffer.AddElement("ObjectDictionaryProp", ObjectDictionaryProp);
            buffer.AddElement("VersionProp", VersionProp);
            buffer.AddElement("DateTimeProp", DateTimeProp);
            buffer.AddElement("GuidProp", GuidProp);
            buffer.AddElement("UriProp", UriProp);
            buffer.AddElement("IPAddressProp", IPAddressProp);
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            PublishManager pubMgr = new PublishManager();

            LogEntry e1 = new LogEntry();
            LogEntry e2;

            byte[] eSerialized = e1.Serialize();

            e2 = new LogEntry(eSerialized);

            pubMgr.Publish(e1.EventType, eSerialized);
        }
    }
}
