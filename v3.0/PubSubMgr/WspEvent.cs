using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Resources;

namespace Microsoft.WebSolutionsPlatform.PubSubManager
{
    /// <summary>
    /// Defines the property type for the header properties
    /// </summary>
    public enum HeaderType : byte
    {
        /// <summary>
        /// EventType
        /// </summary>
        EventType = 0,
        /// <summary>
        /// Originating router
        /// </summary>
        OriginatingRouter = 1,
        /// <summary>
        /// UTC Timestamp Ticks
        /// </summary>
        UtcTimestamp = 2,
        /// <summary>
        /// Version
        /// </summary>
        Version = 3,
        /// <summary>
        /// InRouterName
        /// </summary>
        InRouterName = 4
    }

    /// <summary>
    /// Class describing a Wsp Event
    /// </summary>
    public class WspEvent
    {
        private Guid eventType;
        /// <summary>
        /// Type of the event
        /// </summary>
        public Guid EventType
        {
            get
            {
                return eventType;
            }

            set
            {
                eventType = value;
                Headers[(byte)HeaderType.EventType] = eventType.ToString();
                serializedEvent = null;
            }
        }

        private byte[] serializedEvent = null;
        /// <summary>
        /// A serialized form of the event
        /// </summary>
        public byte[] SerializedEvent
        {
            get
            {
                if (serializedEvent == null)
                {
                    return this.Serialize();
                }
                else
                {
                    return serializedEvent;
                }
            }

            internal set
            {
                serializedEvent = value;
            }
        }

        private string originatingRouterName;
        /// <summary>
        /// Router that the event originated from
        /// </summary>
        public string OriginatingRouterName
        {
            get
            {
                return originatingRouterName;
            }

            set
            {
                originatingRouterName = value;
                Headers[(byte)HeaderType.OriginatingRouter] = originatingRouterName;
                serializedEvent = null;
            }
        }

        private string inRouterName;
        /// <summary>
        /// Router that the event was passed from
        /// </summary>
        public string InRouterName
        {
            get
            {
                return inRouterName;
            }

            set
            {
                inRouterName = value;
                Headers[(byte)HeaderType.InRouterName] = InRouterName;
                serializedEvent = null;
            }
        }

        /// <summary>
        /// Version of the event
        /// </summary>
        public Version Version { get; internal set; }

        private long timeStamp;
        /// <summary>
        /// UTC time in ticks of when the event is published
        /// </summary>
        public long TimeStamp
        {
            get
            {
                return timeStamp;
            }

            set
            {
                timeStamp = value;
                Headers[(byte)HeaderType.UtcTimestamp] = timeStamp.ToString();
                serializedEvent = null;
            }
        }

        /// <summary>
        /// Headers for the event
        /// </summary>
        public Dictionary<byte, string> Headers { get; internal set; }

        private byte[] body;
        /// <summary>
        /// Serialized body of the event
        /// </summary>
        public byte[] Body
        {
            get
            {
                return body;
            }

            set
            {
                body = value;
                serializedEvent = null;
            }
        }

        private static Guid subscriptionEvent = new Guid(@"3D7B4317-C051-4e1a-8379-B6E2D6C107F9");
        /// <summary>
        /// Event type for a Subscription Event
        /// </summary>
        public static Guid SubscriptionEvent
        {
            get
            {
                return subscriptionEvent;
            }

            set
            {
                subscriptionEvent = value;
            }
        }

        /// <summary>
        /// Base contructor to deserialize a byte array into a WspEvent object
        /// </summary>
        internal WspEvent()
        {
            Headers = new Dictionary<byte, string>();

            EventType = Guid.Empty;
            OriginatingRouterName = RouterName;
            InRouterName = RouterName;
            TimeStamp = DateTime.UtcNow.Ticks;

            Version = new Version(version);
            Headers[(byte)HeaderType.Version] = version;

            Body = new byte[0];
        }

        /// <summary>
        /// Base contructor to deserialize a byte array into a WspEvent object
        /// </summary>
        /// <param name="serializationData">Serialized event</param>
        public WspEvent(byte[] serializationData)
        {
            int position = 0;
            Int32 headerLength = 0;
            Int32 totalLength;
            Int32 bodyLength;

            HeaderType headerType;
            Int32 valueLength;

            Headers = new Dictionary<byte, string>();

            SerializedEvent = serializationData;

            totalLength = Read(serializationData, ref position);
            headerLength = Read(serializationData, ref position);

            headerLength += sizeof(Int32) + sizeof(Int32);

            while (position < headerLength)
            {
                headerType = (HeaderType)serializationData[position];
                position++;

                valueLength = Read(serializationData, ref position);

                Headers[(byte)headerType] = Read(serializationData, ref position, valueLength);
            }

            bodyLength = Read(serializationData, ref position);

            Body = new byte[bodyLength];

            Buffer.BlockCopy(serializationData, position, Body, 0, bodyLength);

            eventType = new Guid(Headers[(byte) HeaderType.EventType]);
            originatingRouterName = Headers[(byte)HeaderType.OriginatingRouter];
            inRouterName = Headers[(byte)HeaderType.InRouterName];
            Version = new Version(Headers[(byte)HeaderType.Version]);
            timeStamp = long.Parse(Headers[(byte)HeaderType.UtcTimestamp]);
        }

        /// <summary>
        /// Base contructor create a new WspEvent
        /// </summary>
        /// <param name="eventType">Event type for this object</param>
        /// <param name="extendedHeaders">A dictionary of extended headers</param>
        /// <param name="body">The Body of the event which is normally a serialized object</param>
        public WspEvent(Guid eventType, Dictionary<byte, string> extendedHeaders, byte[] body)
        {
            Headers = new Dictionary<byte, string>();

            EventType = eventType;
            Headers[(byte)HeaderType.EventType] = EventType.ToString();

            OriginatingRouterName = RouterName;
            Headers[(byte)HeaderType.OriginatingRouter] = OriginatingRouterName;

            InRouterName = RouterName;
            Headers[(byte)HeaderType.InRouterName] = InRouterName;

            Version = new Version(version);
            Headers[(byte)HeaderType.Version] = version;

            TimeStamp = DateTime.UtcNow.Ticks;
            timestamp = TimeStamp;
            Headers[(byte)HeaderType.UtcTimestamp] = TimeStamp.ToString();

            Body = body;

            if (extendedHeaders != null && extendedHeaders.Count > 0)
            {
                foreach (byte headerId in extendedHeaders.Keys)
                {
                    if (headerId <= (byte)HeaderType.InRouterName)
                    {
                        continue;
                    }

                    Headers[headerId] = extendedHeaders[headerId];
                }
            }
        }

        private static long timestamp;
        private static byte[] timestampEncodedPriv = null;
        internal static byte[] timestampEncoded
        {
            get
            {
                if (timestampEncodedPriv == null)
                {
                    UTF8Encoding uniEncoding = new UTF8Encoding();
                    timestampEncodedPriv = uniEncoding.GetBytes(timestamp.ToString());
                }

                return timestampEncodedPriv;
            }
        }

        private static string version = "3.0";
        private static byte[] versionEncodedPriv = null;
        internal static byte[] versionEncoded
        {
            get
            {
                if (versionEncodedPriv == null)
                {
                    UTF8Encoding uniEncoding = new UTF8Encoding();
                    versionEncodedPriv = uniEncoding.GetBytes(version);
                }

                return versionEncodedPriv;
            }
        }

        private static string routerName = string.Empty;
        internal static string RouterName
        {
            get
            {
                if (string.IsNullOrEmpty(routerName) == true)
                {
                    routerName = Dns.GetHostName().ToLower();

                    try
                    {
                        char[] splitChar = { '.' };

                        IPHostEntry hostEntry = Dns.GetHostEntry(routerName);

                        string[] temp = hostEntry.HostName.ToLower().Split(splitChar, 2);

                        routerName = temp[0];
                    }
                    catch
                    {
                    }
                }

                return routerName;
            }
        }

        private static byte[] routerNameEncodedPriv = null;
        internal static byte[] routerNameEncoded
        {
            get
            {
                if (routerNameEncodedPriv == null)
                {
                    UTF8Encoding uniEncoding = new UTF8Encoding();
                    routerNameEncodedPriv = uniEncoding.GetBytes(RouterName);
                }

                return routerNameEncodedPriv;
            }
        }

        /// <summary>
        /// Base contructor to deserialize a byte array into a WspEvent object
        /// </summary>
        /// <param name="serializationData">Serialized WspEvent</param>
        /// <param name="inRouterNameArray">A UTF-8 encoded byte array of the InRouterName </param>
        /// <param name="inRouterName">Name of the router that the event was passed in from</param>
        public static WspEvent ChangeInRouterName(byte[] serializationData, byte[] inRouterNameArray, string inRouterName)
        {
            WspEvent wspEvent = new WspEvent();

            int position = 0;
            int positionOfInRouterName = 0;

            Int32 oldTotalLength;
            Int32 newTotalLength;

            Int32 oldHeaderLength = 0;
            Int32 newHeaderLength = 0;

            Int32 oldInRouterNameLength = 0;

            Int32 bodyLength;

            HeaderType headerType;
            Int32 valueLength;

            oldTotalLength = Read(serializationData, ref position);
            oldHeaderLength = Read(serializationData, ref position);

            Int32 headerLength = oldHeaderLength + sizeof(Int32) + sizeof(Int32);

            while (position < headerLength)
            {
                headerType = (HeaderType)serializationData[position];
                position++;

                valueLength = Read(serializationData, ref position);

                if (headerType == HeaderType.InRouterName)
                {
                    oldInRouterNameLength = valueLength;
                    positionOfInRouterName = position;
                    wspEvent.Headers[(byte)headerType] = inRouterName;
                    Read(serializationData, ref position, valueLength);
                }
                else
                {
                    wspEvent.Headers[(byte)headerType] = Read(serializationData, ref position, valueLength);
                }
            }

            bodyLength = Read(serializationData, ref position);

            wspEvent.Body = new byte[bodyLength];

            Buffer.BlockCopy(serializationData, position, wspEvent.Body, 0, bodyLength);

            wspEvent.EventType = new Guid(wspEvent.Headers[(byte)HeaderType.EventType]);
            wspEvent.OriginatingRouterName = wspEvent.Headers[(byte)HeaderType.OriginatingRouter];
            wspEvent.InRouterName = wspEvent.Headers[(byte)HeaderType.InRouterName];
            wspEvent.Version = new Version(wspEvent.Headers[(byte)HeaderType.Version]);
            wspEvent.TimeStamp = long.Parse(wspEvent.Headers[(byte)HeaderType.UtcTimestamp]);

            newTotalLength = oldTotalLength + (inRouterNameArray.Length - oldInRouterNameLength);
            newHeaderLength = oldHeaderLength + (inRouterNameArray.Length - oldInRouterNameLength);

            wspEvent.SerializedEvent = new byte[newTotalLength];

            int location = 0;
            int copyLength;

            Buffer.BlockCopy(BitConverter.GetBytes((Int32)newTotalLength), 0, wspEvent.SerializedEvent, location, sizeof(Int32));
            location += sizeof(Int32);

            Buffer.BlockCopy(BitConverter.GetBytes((Int32)newHeaderLength), 0, wspEvent.SerializedEvent, location, sizeof(Int32));
            location += sizeof(Int32);

            copyLength = positionOfInRouterName - sizeof(Int32) - location;

            Buffer.BlockCopy(serializationData, location, wspEvent.SerializedEvent, location, copyLength);
            location += copyLength;

            Buffer.BlockCopy(BitConverter.GetBytes((Int32)inRouterNameArray.Length), 0, wspEvent.SerializedEvent, location, sizeof(Int32));
            location += sizeof(Int32);

            Buffer.BlockCopy(inRouterNameArray, 0, wspEvent.SerializedEvent, location, inRouterNameArray.Length);
            location += inRouterNameArray.Length;

            copyLength = newTotalLength - location;

            Buffer.BlockCopy(serializationData, oldTotalLength - copyLength, wspEvent.SerializedEvent, location, copyLength);
            location += copyLength;

            return wspEvent;
        }

        /// <summary>
        /// Gets only the standard headers from the serialized WspEvent
        /// </summary>
        /// <param name="serializationData">Serialized WspEvent object</param>
        /// <param name="eventType">The event type for the serialized object</param>
        /// <param name="originatingRouterName">The originating router name for the serialized object</param>
        /// <param name="inRouterName">The router that the event was passed in from</param>
        /// <param name="version">The version type for the serialized object</param>
        /// <param name="utcTimeStamp">The UTC timestamp ticks for the serialized object</param>
        public static void GetStandardHeaders(byte[] serializationData, out Guid eventType, out string originatingRouterName, out string inRouterName, out Version version, out long utcTimeStamp)
        {
            int position = 0;
            Int32 headerLength = 0;
            Int32 totalLength;

            HeaderType headerType;
            Int32 valueLength;

            totalLength = Read(serializationData, ref position);
            headerLength = Read(serializationData, ref position);

            headerType = (HeaderType)serializationData[position];
            position++;
            valueLength = Read(serializationData, ref position);
            eventType = new Guid(Read(serializationData, ref position, valueLength));

            headerType = (HeaderType)serializationData[position];
            position++;
            valueLength = Read(serializationData, ref position);
            originatingRouterName = Read(serializationData, ref position, valueLength);

            headerType = (HeaderType)serializationData[position];
            position++;
            valueLength = Read(serializationData, ref position);
            inRouterName = Read(serializationData, ref position, valueLength);

            headerType = (HeaderType)serializationData[position];
            position++;
            valueLength = Read(serializationData, ref position);
            version = new Version(Read(serializationData, ref position, valueLength));

            headerType = (HeaderType)serializationData[position];
            position++;
            valueLength = Read(serializationData, ref position);
            utcTimeStamp = long.Parse(Read(serializationData, ref position, valueLength));

            return;
        }

        /// <summary>
        /// Serializes the event
        /// </summary>
        /// <returns>Byte array of event</returns>
        public byte[] Serialize()
        {
            Int32 headerLength = 0;
            Int32 totalLength;
            byte[] eventTypeSerialized;
            Dictionary<byte, byte[]> extendedHeadersPriv = null;

            Headers = new Dictionary<byte, string>();

            Headers[(byte)HeaderType.EventType] = EventType.ToString();

            OriginatingRouterName = RouterName;
            Headers[(byte)HeaderType.OriginatingRouter] = OriginatingRouterName;

            Headers[(byte)HeaderType.InRouterName] = InRouterName;

            Version = new Version(version);
            Headers[(byte)HeaderType.Version] = version;

            TimeStamp = DateTime.UtcNow.Ticks;
            timestamp = TimeStamp;
            Headers[(byte)HeaderType.UtcTimestamp] = TimeStamp.ToString();

            UTF8Encoding uniEncoding = new UTF8Encoding();
            eventTypeSerialized = uniEncoding.GetBytes(EventType.ToString());

            headerLength += 1 + sizeof(Int32) + eventTypeSerialized.Length;
            headerLength += 1 + sizeof(Int32) + routerNameEncoded.Length;
            headerLength += 1 + sizeof(Int32) + routerNameEncoded.Length;
            headerLength += 1 + sizeof(Int32) + timestampEncoded.Length;
            headerLength += 1 + sizeof(Int32) + versionEncoded.Length;

            extendedHeadersPriv = new Dictionary<byte, byte[]>();

            foreach (byte headerId in Headers.Keys)
            {
                if (headerId <= (byte)HeaderType.InRouterName)
                {
                    continue;
                }

                extendedHeadersPriv[headerId] = uniEncoding.GetBytes(Headers[headerId]);

                headerLength += 1 + sizeof(Int32) + extendedHeadersPriv[headerId].Length;
            }

            totalLength = headerLength + sizeof(Int32) + Body.Length + sizeof(Int32) + sizeof(Int32);

            SerializedEvent = new byte[totalLength];

            int location = 0;

            Buffer.BlockCopy(BitConverter.GetBytes((Int32)totalLength), 0, SerializedEvent, location, sizeof(Int32));
            location += sizeof(Int32);

            Buffer.BlockCopy(BitConverter.GetBytes((Int32)headerLength), 0, SerializedEvent, location, sizeof(Int32));
            location += sizeof(Int32);

            SerializedEvent[location] = (byte)HeaderType.EventType;
            location += sizeof(byte);

            Buffer.BlockCopy(BitConverter.GetBytes((Int32)eventTypeSerialized.Length), 0, SerializedEvent, location, sizeof(Int32));
            location += sizeof(Int32);

            Buffer.BlockCopy(eventTypeSerialized, 0, SerializedEvent, location, eventTypeSerialized.Length);
            location += eventTypeSerialized.Length;

            SerializedEvent[location] = (byte)HeaderType.OriginatingRouter;
            location += sizeof(byte);

            Buffer.BlockCopy(BitConverter.GetBytes((Int32)routerNameEncoded.Length), 0, SerializedEvent, location, sizeof(Int32));
            location += sizeof(Int32);

            Buffer.BlockCopy(routerNameEncoded, 0, SerializedEvent, location, routerNameEncoded.Length);
            location += routerNameEncoded.Length;

            SerializedEvent[location] = (byte)HeaderType.InRouterName;
            location += sizeof(byte);

            Buffer.BlockCopy(BitConverter.GetBytes((Int32)routerNameEncoded.Length), 0, SerializedEvent, location, sizeof(Int32));
            location += sizeof(Int32);

            Buffer.BlockCopy(routerNameEncoded, 0, SerializedEvent, location, routerNameEncoded.Length);
            location += routerNameEncoded.Length;

            SerializedEvent[location] = (byte)HeaderType.UtcTimestamp;
            location += sizeof(byte);

            Buffer.BlockCopy(BitConverter.GetBytes((Int32)timestampEncoded.Length), 0, SerializedEvent, location, sizeof(Int32));
            location += sizeof(Int32);

            Buffer.BlockCopy(timestampEncoded, 0, SerializedEvent, location, timestampEncoded.Length);
            location += timestampEncoded.Length;

            SerializedEvent[location] = (byte)HeaderType.Version;
            location += sizeof(byte);

            Buffer.BlockCopy(BitConverter.GetBytes((Int32)versionEncoded.Length), 0, SerializedEvent, location, sizeof(Int32));
            location += sizeof(Int32);

            Buffer.BlockCopy(versionEncoded, 0, SerializedEvent, location, versionEncoded.Length);
            location += versionEncoded.Length;

            if (extendedHeadersPriv != null)
            {
                foreach (byte headerId in extendedHeadersPriv.Keys)
                {
                    if (headerId <= (byte)HeaderType.InRouterName)
                    {
                        continue;
                    }

                    SerializedEvent[location] = headerId;
                    location += sizeof(byte);

                    Buffer.BlockCopy(BitConverter.GetBytes((Int32)extendedHeadersPriv[headerId].Length), 0, SerializedEvent, location, sizeof(Int32));
                    location += sizeof(Int32);

                    Buffer.BlockCopy(extendedHeadersPriv[headerId], 0, SerializedEvent, location, extendedHeadersPriv[headerId].Length);
                    location += extendedHeadersPriv[headerId].Length;
                }
            }

            Buffer.BlockCopy(BitConverter.GetBytes((Int32)Body.Length), 0, SerializedEvent, location, sizeof(Int32));
            location += sizeof(Int32);

            Buffer.BlockCopy(Body, 0, SerializedEvent, location, Body.Length);
            location += Body.Length;

            return SerializedEvent;
        }

        /// <summary>
        /// Read Int32 from buffer
        /// </summary>
        /// <param name="buffer">Buffer to read value from</param>
        /// <param name="position">Position in buffer to read from</param>
        internal static Int32 Read(byte[] buffer, ref int position)
        {
            byte[] arrayOut = new byte[sizeof(Int32)];

            Buffer.BlockCopy(buffer, position, arrayOut, 0, sizeof(Int32));

            position += sizeof(Int32);

            return BitConverter.ToInt32(arrayOut, 0);
        }

        /// <summary>
        /// Read String from buffer
        /// </summary>
        /// <param name="buffer">Buffer to read value from</param>
        /// <param name="position">Position in buffer to read from</param>
        /// <param name="length">Length of bytes for string</param>
        internal static string Read(byte[] buffer, ref int position, int length)
        {
            byte[] arrayOut = new byte[length];

            Buffer.BlockCopy(buffer, position, arrayOut, 0, length);

            position += length;

            return Encoding.UTF8.GetString(arrayOut);
        }
    }
}
