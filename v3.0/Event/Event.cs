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

namespace Microsoft.WebSolutionsPlatform.Event
{
    /// <summary>
    /// Base class for events. All events must inherit from this class.
    /// </summary>
    abstract public class WspBody
	{
		private WspBuffer serializedBody = null;

        /// <summary>
        /// Type of the event
        /// </summary>
        public Guid EventType { get; set; }

        /// <summary>
		/// Serialized version of the WspBody
		/// </summary>
        public WspBuffer SerializedBody
		{
			get
			{
				return serializedBody;
			}

            set
            {
                serializedBody = value;
            }
        }

		/// <summary>
		/// Base constructor to create a new event
		/// </summary>
        public WspBody()
		{
		}

		/// <summary>
		/// Base contructor to re-instantiate an existing event
		/// </summary>
        /// <param name="serializationData">Serialized event buffer</param>
        public WspBody(byte[] serializationData)
		{
            Deserialize(serializationData);
		}

        /// <summary>
        /// Generic method to set a property on an object.
        /// </summary>
        /// <param name="elementName">Property name to be set</param>
        /// <param name="elementValue">Value object</param>
        /// <returns>true if success and false if failed</returns>
        virtual public bool SetElement(string elementName, object elementValue)
        {
            bool rc = true;
            PropertyInfo prop;

            prop = this.GetType().GetProperty(elementName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            if (prop == null)
            {
                rc = false;
            }
            else
            {
                try
                {
                    prop.SetValue(this, elementValue, null);
                }
                catch
                {
                    rc = false;
                }
            }

            return rc;
        }

		/// <summary>
		/// Serializes the event and puts it in the SerializedEvent property
		/// </summary>
        /// <returns>Serialized version of the event</returns>
		public byte[] Serialize()
		{
            if (serializedBody == null)
            {
                serializedBody = new WspBuffer();
            }

            serializedBody.Reset();

            GetObjectData(serializedBody);

            return serializedBody.ToByteArray();
		}

        /// <summary>
        /// Used for event serialization.
        /// </summary>
        /// <param name="buffer">SerializationData object passed to store serialized object</param>
        abstract public void GetObjectData(WspBuffer buffer);

		/// <summary>
		/// Deserializes the event
		/// </summary>
		public virtual void Deserialize( byte[] serializationData )
		{
            string propName;
            byte propType;

            string stringValue = string.Empty;
            byte byteValue = 0;
            SByte sbyteValue = 0;
            byte[] byteArrayValue = null;
            char charValue = Char.MinValue;
            char[] charArrayValue = null;
            bool boolValue = false;
            Int16 int16Value = 0;
            Int32 int32Value = 0;
            Int64 int64Value = 0;
            UInt16 uint16Value = 0;
            UInt32 uint32Value = 0;
            UInt64 uint64Value = 0;
            Single singleValue = 0;
            Double doubleValue = 0;
            Decimal decimalValue = 0;
            Version versionValue = null;
            DateTime dateTimeValue = DateTime.MinValue;
            Guid guidValue = Guid.Empty;
            IPAddress ipAddressValue = null;
            Uri uriValue = null;
            Dictionary<string, string> stringDictionaryValue = null;
            Dictionary<string, object> objectDictionaryValue = null;
            List<string> stringListValue = null;
            List<object> objectListValue = null;

            serializedBody = new WspBuffer(serializationData);

            while (serializedBody.Position < serializedBody.Size)
            {
                if (serializedBody.Read(out propName) == false)
                {
                    throw new EventDeserializationException("Error reading PropertyName from buffer");
                }

                if (serializedBody.Read(out propType) == false)
                {
                    throw new EventDeserializationException("Error reading PropertyType from buffer");
                }

                switch (propType)
                {
                    case (byte)PropertyType.String:
                        if (serializedBody.Read(out stringValue) == false)
                        {
                            throw new EventDeserializationException("Error reading PropertyType from buffer");
                        }

                        SetElement(propName, stringValue);

                        continue;

                    case (byte)PropertyType.Boolean:
                        if (serializedBody.Read(out boolValue) == false)
                        {
                            throw new EventDeserializationException("Error reading PropertyType from buffer");
                        }

                        SetElement(propName, boolValue);

                        continue;

                    case (byte)PropertyType.Int32:
                        if (serializedBody.Read(out int32Value) == false)
                        {
                            throw new EventDeserializationException("Error reading PropertyType from buffer");
                        }

                        SetElement(propName, int32Value);

                        continue;

                    case (byte)PropertyType.Int64:
                        if (serializedBody.Read(out int64Value) == false)
                        {
                            throw new EventDeserializationException("Error reading PropertyType from buffer");
                        }

                        SetElement(propName, int64Value);

                        continue;

                    case (byte)PropertyType.SByte:
                        if (serializedBody.Read(out sbyteValue) == false)
                        {
                            throw new EventDeserializationException("Error reading PropertyType from buffer");
                        }

                        SetElement(propName, sbyteValue);

                        continue;

                    case (byte)PropertyType.Double:
                        if (serializedBody.Read(out doubleValue) == false)
                        {
                            throw new EventDeserializationException("Error reading PropertyType from buffer");
                        }

                        SetElement(propName, doubleValue);

                        continue;

                    case (byte)PropertyType.Decimal:
                        if (serializedBody.Read(out decimalValue) == false)
                        {
                            throw new EventDeserializationException("Error reading PropertyType from buffer");
                        }

                        SetElement(propName, decimalValue);

                        continue;

                    case (byte)PropertyType.Byte:
                        if (serializedBody.Read(out byteValue) == false)
                        {
                            throw new EventDeserializationException("Error reading PropertyType from buffer");
                        }

                        SetElement(propName, byteValue);

                        continue;

                    case (byte)PropertyType.Char:
                        if (serializedBody.Read(out charValue) == false)
                        {
                            throw new EventDeserializationException("Error reading PropertyType from buffer");
                        }

                        SetElement(propName, charValue);

                        continue;

                    case (byte)PropertyType.Version:
                        if (serializedBody.Read(out versionValue) == false)
                        {
                            throw new EventDeserializationException("Error reading PropertyType from buffer");
                        }

                        SetElement(propName, versionValue);

                        continue;

                    case (byte)PropertyType.DateTime:
                        if (serializedBody.Read(out dateTimeValue) == false)
                        {
                            throw new EventDeserializationException("Error reading PropertyType from buffer");
                        }

                        SetElement(propName, dateTimeValue);

                        continue;

                    case (byte)PropertyType.Guid:
                        if (serializedBody.Read(out guidValue) == false)
                        {
                            throw new EventDeserializationException("Error reading PropertyType from buffer");
                        }

                        SetElement(propName, guidValue);

                        continue;

                    case (byte)PropertyType.Uri:
                        if (serializedBody.Read(out uriValue) == false)
                        {
                            throw new EventDeserializationException("Error reading PropertyType from buffer");
                        }

                        SetElement(propName, uriValue);

                        continue;

                    case (byte)PropertyType.Int16:
                        if (serializedBody.Read(out int16Value) == false)
                        {
                            throw new EventDeserializationException("Error reading PropertyType from buffer");
                        }

                        SetElement(propName, int16Value);

                        continue;

                    case (byte)PropertyType.Single:
                        if (serializedBody.Read(out singleValue) == false)
                        {
                            throw new EventDeserializationException("Error reading PropertyType from buffer");
                        }

                        SetElement(propName, singleValue);

                        continue;

                    case (byte)PropertyType.UInt16:
                        if (serializedBody.Read(out uint16Value) == false)
                        {
                            throw new EventDeserializationException("Error reading PropertyType from buffer");
                        }

                        SetElement(propName, uint16Value);

                        continue;

                    case (byte)PropertyType.UInt32:
                        if (serializedBody.Read(out uint32Value) == false)
                        {
                            throw new EventDeserializationException("Error reading PropertyType from buffer");
                        }

                        SetElement(propName, uint32Value);

                        continue;

                    case (byte)PropertyType.UInt64:
                        if (serializedBody.Read(out uint64Value) == false)
                        {
                            throw new EventDeserializationException("Error reading PropertyType from buffer");
                        }

                        SetElement(propName, uint64Value);

                        continue;

                    case (byte)PropertyType.IPAddress:
                        if (serializedBody.Read(out ipAddressValue) == false)
                        {
                            throw new EventDeserializationException("Error reading PropertyType from buffer");
                        }

                        SetElement(propName, ipAddressValue);

                        continue;

                    case (byte)PropertyType.ByteArray:
                        if (serializedBody.Read(out byteArrayValue) == false)
                        {
                            throw new EventDeserializationException("Error reading PropertyType from buffer");
                        }

                        SetElement(propName, byteArrayValue);

                        continue;

                    case (byte)PropertyType.CharArray:
                        if (serializedBody.Read(out charArrayValue) == false)
                        {
                            throw new EventDeserializationException("Error reading PropertyType from buffer");
                        }

                        SetElement(propName, charArrayValue);

                        continue;

                    case (byte)PropertyType.StringDictionary:
                        if (serializedBody.Read(out stringDictionaryValue) == false)
                        {
                            throw new EventDeserializationException("Error reading PropertyType from buffer");
                        }

                        SetElement(propName, stringDictionaryValue);

                        continue;

                    case (byte)PropertyType.ObjectDictionary:
                        if (serializedBody.Read(out objectDictionaryValue) == false)
                        {
                            throw new EventDeserializationException("Error reading PropertyType from buffer");
                        }

                        SetElement(propName, objectDictionaryValue);

                        continue;

                    case (byte)PropertyType.StringList:
                        if (serializedBody.Read(out stringListValue) == false)
                        {
                            throw new EventDeserializationException("Error reading PropertyType from buffer");
                        }

                        SetElement(propName, stringListValue);

                        continue;

                    case (byte)PropertyType.ObjectList:
                        if (serializedBody.Read(out objectListValue) == false)
                        {
                            throw new EventDeserializationException("Error reading PropertyType from buffer");
                        }

                        SetElement(propName, objectListValue);

                        continue;

                    default:
                        throw new EventTypeNotSupportedException("Cannot deserialize type of Value object");
                }
            }
		}
	}
}
