using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Resources;
using System.Reflection;

namespace Microsoft.WebSolutionsPlatform.Event
{
    /// <summary>
    /// Defines the property types supported by the base event class.
    /// </summary>
	public enum PropertyType : byte
	{
        /// <summary>
        /// This property type will be ignored.
        /// </summary>
        None = 0,
        /// <summary>
        /// Boolean type
        /// </summary>
		Boolean = 1,
        /// <summary>
        /// byte type
        /// </summary>
		Byte = 2,
        /// <summary>
        /// byte[] type
        /// </summary>
        ByteArray = 3,
        /// <summary>
        /// char type
        /// </summary>
        Char = 4,
        /// <summary>
        /// char[] type
        /// </summary>
        CharArray = 5,
        /// <summary>
        /// Decimal type
        /// </summary>
        Decimal = 6,
        /// <summary>
        /// Double type
        /// </summary>
        Double = 7,
        /// <summary>
        /// Int16 type
        /// </summary>
        Int16 = 8,
        /// <summary>
        /// Int32 type
        /// </summary>
        Int32 = 9,
        /// <summary>
        /// Int64 type
        /// </summary>
        Int64 = 10,
        /// <summary>
        /// SByte type
        /// </summary>
        SByte = 11,
        /// <summary>
        /// Single type
        /// </summary>
        Single = 12,
        /// <summary>
        /// String type
        /// </summary>
        String = 13,
        /// <summary>
        /// UInt16 type
        /// </summary>
        UInt16 = 14,
        /// <summary>
        /// UInt32 type
        /// </summary>
        UInt32 = 15,
        /// <summary>
        /// UInt64 type
        /// </summary>
        UInt64 = 16,
        /// <summary>
        /// Version type
        /// </summary>
        Version = 17,
        /// <summary>
        /// DateTime type
        /// </summary>
        DateTime = 18,
        /// <summary>
        /// Guid type
        /// </summary>
        Guid = 19,
        /// <summary>
        /// Uri type
        /// </summary>
        Uri = 20,
        /// <summary>
        /// IPAddress type
        /// </summary>
        IPAddress = 21,
        /// <summary>
        /// Generic Dictionary type
        /// </summary>
        Dictionary = 22
	}

    /// <summary>
    /// This class is used to create and enumerate a serialized event object.
    /// </summary>
    public class SerializationData : IEnumerable<WspKeyValuePair<string, object>>, IEnumerator<WspKeyValuePair<string, object>>
	{
        private bool disposed;

        private MemoryStream dataStream;
		private BinaryReader dataReader;
		private BinaryWriter dataWriter;
		private static Dictionary<string, byte> objectType;
        private static object objectTypeAccess = new object();

		private WspKeyValuePair<string, object> current;
		/// <summary>
		/// Current element in the SerializationData object.
		/// </summary>
		public WspKeyValuePair<string, object> Current
		{
			get
			{
				return current;
			}
        }

		object System.Collections.IEnumerator.Current
		{
			get
			{
				return (object) current;
			}
		}

		private bool readMode;
		/// <summary>
		/// Identifies if object can be read.  When state changes to true, the position
		/// of the stream is reset.
		/// </summary>
		public bool ReadMode
		{
			get
			{
				return readMode;
			}
			set
			{
				readMode = value;

				if(readMode == true)
				{
					writeMode = false;

					this.Reset();
				}
				else
				{
					WriteMode = true;
				}
			}
		}

		private bool writeMode;
		/// <summary>
		/// Identifies if object can be written.  When state changes to true, the position
		/// of the stream is set to the end.
		/// </summary>
		public bool WriteMode
		{
			get
			{
				return writeMode;
			}
			set
			{
				writeMode = value;

				if(writeMode == true)
				{
					readMode = false;

					dataReader.BaseStream.Seek(0, SeekOrigin.End);
				}
				else
				{
					ReadMode = true;
				}
			}
		}

		/// <summary>
		/// Length of the SerializationData object
		/// </summary>
		public long Length
		{
			get
			{
				return dataStream.Length;
			}
		}

        /// <summary>
        /// Constructor
        /// </summary>
		public SerializationData()
		{
			LoadObjectType();

			current = new WspKeyValuePair<string, object>();

			dataStream = new MemoryStream();

			writeMode = true;

			dataWriter = new BinaryWriter(dataStream, Encoding.UTF8);

			dataReader = new BinaryReader(dataStream, Encoding.UTF8);
		}

        /// <summary>
        /// Constructor which can load and parse a serialized event object. The properties can 
        /// then be enumerated with a foreach statement.
        /// </summary>
        /// <param name="inData">Serialized event object.</param>
		public SerializationData( byte[] inData )
		{
			LoadObjectType();

			current = new WspKeyValuePair<string, object>();

            dataStream = new MemoryStream();
            
			writeMode = true;

			dataWriter = new BinaryWriter(dataStream, Encoding.UTF8);
			dataWriter.Write(inData);

            dataReader = new BinaryReader(dataStream, Encoding.UTF8);

            dataReader.BaseStream.Position = 0;

			ReadMode = true;
		}

		/// <summary>
		/// Adds a prefix to the SerializationData object.
		/// </summary>
		/// <param name="value">Value of the element being added</param>
		internal void AddPrefix( Guid value )
		{
			dataWriter.Write(value.ToString());
		}

		/// <summary>
		/// Adds a prefix to the SerializationData object.
		/// </summary>
		/// <param name="value">Value of the element being added</param>
		internal void AddPrefix( string value )
		{
			dataWriter.Write(value);
		}

		/// <summary>
		/// Method to add element data to the SerializationData object.
		/// </summary>
		/// <param name="key">Key of the element being added</param>
		/// <param name="value">Value of the element being added</param>
		public void AddElement( string key, object value )
		{
            if (WriteMode == false)
            {
                ResourceManager rm = new ResourceManager("WspEvent.WspEvent", Assembly.GetExecutingAssembly());

                throw new EventSerializationException(rm.GetString("InReadState"));
            }

			dataWriter.Write(key);

			byte valueType = GetObjectType(value);

			dataWriter.Write(valueType);

			switch(valueType)
			{
				case (byte)PropertyType.String:
                    if (value == null)
                    {
                        dataWriter.Write(string.Empty);
                    }
                    else
                    {
                        dataWriter.Write(value.ToString());
                    }

					break;

				case (byte)PropertyType.Boolean:
					dataWriter.Write((bool)value);
					break;

				case (byte)PropertyType.Int32:
					dataWriter.Write((Int32)value);
					break;

				case (byte)PropertyType.Int64:
					dataWriter.Write((Int64)value);
					break;

				case (byte)PropertyType.Double:
					dataWriter.Write((Double)value);
					break;

				case (byte)PropertyType.Decimal:
					dataWriter.Write((Decimal)value);
					break;

				case (byte)PropertyType.Byte:
					dataWriter.Write((Byte)value);
					break;

				case (byte)PropertyType.Char:
					dataWriter.Write((Char)value);
					break;

				case (byte)PropertyType.Version:
                    if (value == null)
                    {
                        dataWriter.Write(string.Empty);
                    }
                    else
                    {
                        dataWriter.Write(value.ToString());
                    }

                    break;

				case (byte)PropertyType.DateTime:
                    if (value == null)
                    {
                        dataWriter.Write(((long)0).ToString());
                    }
                    else
                    {
                        dataWriter.Write(((DateTime)value).Ticks.ToString());
                    }

                    break;

				case (byte)PropertyType.Guid:
                    if (value == null)
                    {
                        dataWriter.Write(Guid.Empty.ToString());
                    }
                    else
                    {
                        dataWriter.Write(value.ToString());
                    }

                    break;

				case (byte)PropertyType.Uri:
                    if (value == null)
                    {
                        dataWriter.Write(@"http://EmptyUri");
                    }
                    else
                    {
                        dataWriter.Write(value.ToString());
                    }

                    break;

				case (byte)PropertyType.Int16:
					dataWriter.Write((Int16)value);
					break;

				case (byte)PropertyType.SByte:
					dataWriter.Write((SByte)value);
					break;

				case (byte)PropertyType.Single:
					dataWriter.Write((Single)value);
					break;

				case (byte)PropertyType.UInt16:
					dataWriter.Write((UInt16)value);
					break;

				case (byte)PropertyType.UInt32:
					dataWriter.Write((UInt32)value);
					break;

				case (byte)PropertyType.UInt64:
					dataWriter.Write((UInt64)value);
					break;

				case (byte)PropertyType.IPAddress:
                    if (value == null)
                    {
                        dataWriter.Write(@"0.0.0.0");
                    }
                    else
                    {
                        dataWriter.Write((string)value.ToString());
                    }

                    break;

				case (byte)PropertyType.ByteArray:
                    if (value == null)
                    {
                        Byte[] emptyByteArray = new byte[] { };
                        System.Text.Encoding encoding = System.Text.Encoding.UTF8;
                        dataWriter.Write(encoding.GetString((Byte[])emptyByteArray));
                    }
                    else
                    {
                        System.Text.Encoding encoding = System.Text.Encoding.UTF8;
                        dataWriter.Write(encoding.GetString((Byte[])value));
                    }

                    break;

				case (byte)PropertyType.CharArray:
                    if (value == null)
                    {
                        dataWriter.Write(0);
                    }
                    else
                    {
                        char[] charValue = (char[])value;
                        dataWriter.Write(charValue.Length);
                        dataWriter.Write(charValue);
                    }

                    break;

				case (byte)PropertyType.Dictionary:
                    if (value == null)
                    {
                        dataWriter.Write(0);
                    }
                    else
                    {
                        dataWriter.Write((Int32)(((IDictionary)value).Count));
                        foreach (object oKey in ((IDictionary)value).Keys)
                        {
                            AddElement("key", oKey);
                            AddElement("value", ((IDictionary)value)[oKey]);
                        }
                    }

                    break;

				default:
                    ResourceManager rm = new ResourceManager("WspEvent.WspEvent", Assembly.GetExecutingAssembly());

                    throw new EventTypeNotSupportedException(rm.GetString("CannotSerialize"));
			}
		}

        /// <summary>
        /// Resets the underlying memory stream.
        /// </summary>
        public void ResetStream()
        {
            dataStream.Position = 0;
            dataStream.SetLength(0);
        }

		/// <summary>
		/// Method to set the enumerator to its initial position, before the first key/value pair 
		/// data in the SerializationData object.
		/// </summary>
		public void Reset()
		{
            if (ReadMode == false)
            {
                ResourceManager rm = new ResourceManager("WspEvent.WspEvent", Assembly.GetExecutingAssembly());

                throw new EventSerializationException(rm.GetString("MustBeInReadState"));
            }

			dataReader.BaseStream.Seek(0, SeekOrigin.Begin);
            dataReader.BaseStream.Position = 0;

			//if(dataReader.BaseStream.Length > 0)
			//	dataReader.ReadInt32();

			current.Key = null;
			current.ValueIn = null;
		}

		/// <summary>
		/// Method to move to the next key/value pair data in the SerializationData object.
		/// </summary>
		public bool MoveNext()
		{
			byte valueType;
            try
            {
                if (ReadMode == false)
                {
                    ResourceManager rm = new ResourceManager("WspEvent.WspEvent", Assembly.GetExecutingAssembly());

                    throw new EventSerializationException(rm.GetString("InWriteState"));
                }

                if (dataReader.BaseStream.Position == dataReader.BaseStream.Length)
                {
                    return false;
                }
                else
                {
                    current.Key = dataReader.ReadString();
                }

                valueType = dataReader.ReadByte();

                switch (valueType)
                {
                    case (byte)PropertyType.String:
                        current.ValueIn = dataReader.ReadString();
                        break;

                    case (byte)PropertyType.Boolean:
                        current.ValueIn = dataReader.ReadBoolean();
                        break;

                    case (byte)PropertyType.Int32:
                        current.ValueIn = dataReader.ReadInt32();
                        break;

                    case (byte)PropertyType.Int64:
                        current.ValueIn = dataReader.ReadInt64();
                        break;

                    case (byte)PropertyType.Double:
                        current.ValueIn = dataReader.ReadDouble();
                        break;

                    case (byte)PropertyType.Decimal:
                        current.ValueIn = dataReader.ReadDecimal();
                        break;

                    case (byte)PropertyType.Byte:
                        current.ValueIn = dataReader.ReadByte();
                        break;

                    case (byte)PropertyType.Char:
                        current.ValueIn = dataReader.ReadChar();
                        break;

                    case (byte)PropertyType.Version:
                        current.ValueIn = new Version(dataReader.ReadString());
                        break;

                    case (byte)PropertyType.DateTime:
                        current.ValueIn = new DateTime(Int64.Parse(dataReader.ReadString()));
                        break;

                    case (byte)PropertyType.Guid:
                        current.ValueIn = new Guid(dataReader.ReadString());
                        break;

                    case (byte)PropertyType.Uri:
                        current.ValueIn = new Uri(dataReader.ReadString());
                        break;

                    case (byte)PropertyType.Int16:
                        current.ValueIn = dataReader.ReadInt16();
                        break;

                    case (byte)PropertyType.SByte:
                        current.ValueIn = dataReader.ReadSByte();
                        break;

                    case (byte)PropertyType.Single:
                        current.ValueIn = dataReader.ReadSingle();
                        break;

                    case (byte)PropertyType.UInt16:
                        current.ValueIn = dataReader.ReadUInt16();
                        break;

                    case (byte)PropertyType.UInt32:
                        current.ValueIn = dataReader.ReadUInt32();
                        break;

                    case (byte)PropertyType.UInt64:
                        current.ValueIn = dataReader.ReadUInt64();
                        break;

                    case (byte)PropertyType.IPAddress:
                        current.ValueIn = IPAddress.Parse(dataReader.ReadString());
                        break;

                    case (byte)PropertyType.ByteArray:
                        System.Text.Encoding encoding = System.Text.Encoding.Unicode;
                        current.ValueIn = encoding.GetBytes(dataReader.ReadString());
                        break;

                    case (byte)PropertyType.CharArray:
                        int arrayLength = dataReader.ReadInt32();
                        current.ValueIn = dataReader.ReadChars(arrayLength);
                        break;

                    case (byte)PropertyType.Dictionary:
                        current.ValueIn = dataReader.ReadInt32();
                        break;

                    default:
                        ResourceManager rm = new ResourceManager("WspEvent.WspEvent", Assembly.GetExecutingAssembly());

                        throw new EventTypeNotSupportedException(rm.GetString("CannotDeserialize"));
                }

                return true;
            }
            catch
            {
                return false;
            }
		}

        /// <summary>
        /// Returns the OriginatingRouterName for the Event
        /// </summary>
        public string GetOriginatingRouterName()
        {
            dataReader.BaseStream.Position = 0;
            return dataReader.ReadString();
        }

        /// <summary>
        /// Returns the InRouterName for the Event
        /// </summary>
        public string GetInRouterName()
        {
            return dataReader.ReadString();
        }

        /// <summary>
        /// Returns the EventType for the Event
        /// </summary>
        public Guid GetEventType()
        {
            string guid = dataReader.ReadString();
            return new Guid(guid);
        }

		/// <summary>
		/// Returns an enumerator for the object.
		/// </summary>
		public IEnumerator<WspKeyValuePair<string, object>> GetEnumerator()
		{
			return this;
		}

		/// <summary>
		/// Returns an enumerator for the object.
		/// </summary>
		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
			return this;
		}

		/// <summary>
		/// Disposes of the object.
		/// </summary>
		public void Dispose()
		{
            // Do nothing
        }

        /// <summary>
        /// Disposes of the object.
        /// </summary>
        protected virtual void Dispose(bool disposing) 
        {
            if (!disposed)
            {
                dataStream.Close();
                dataReader.Close();
                dataWriter.Close();

                disposed = true;

                if (disposing)
                {
                    GC.SuppressFinalize(this);
                }
            }
        }

        /// <summary>
        /// Destructor
        /// </summary>
        ~SerializationData()
        {
            Dispose(false);
        }

		/// <summary>
		/// Return the object as a string.
		/// </summary>
		public override string ToString()
		{
            if (ReadMode == false)
            {
                ResourceManager rm = new ResourceManager("WspEvent.WspEvent", Assembly.GetExecutingAssembly());

                throw new EventSerializationException(rm.GetString("MustBeInReadStateForToString"));
            }

			dataReader.BaseStream.Seek(0, SeekOrigin.Begin);

			return dataReader.ToString();
		}

		/// <summary>
		/// Return the object as a byte array in UTF-8 format.
		/// </summary>
		public byte[] ToBytes()
		{
            if (ReadMode == false)
            {
                ResourceManager rm = new ResourceManager("WspEvent.WspEvent", Assembly.GetExecutingAssembly());

                throw new EventSerializationException(rm.GetString("MustBeInReadStateForToBytes"));
            }

			dataReader.BaseStream.Seek(0, SeekOrigin.Begin);
            dataReader.BaseStream.Position = 0;

			return dataReader.ReadBytes((int)dataStream.Length);
		}

		private static void LoadObjectType()
		{
			if(objectType == null)
			{
                lock (objectTypeAccess)
                {
                    if (objectType == null)
                    {
                        Dictionary<string, byte> oType = new Dictionary<string, byte>();

                        oType.Add(@"System.String", (byte)PropertyType.String);
                        oType.Add(@"System.Boolean", (byte)PropertyType.Boolean);
                        oType.Add(@"System.Int16", (byte)PropertyType.Int16);
                        oType.Add(@"System.Int32", (byte)PropertyType.Int32);
                        oType.Add(@"System.Int64", (byte)PropertyType.Int64);
                        oType.Add(@"System.UInt16", (byte)PropertyType.UInt16);
                        oType.Add(@"System.UInt32", (byte)PropertyType.UInt32);
                        oType.Add(@"System.UInt64", (byte)PropertyType.UInt64);
                        oType.Add(@"System.Double", (byte)PropertyType.Double);
                        oType.Add(@"System.Decimal", (byte)PropertyType.Decimal);
                        oType.Add(@"System.Byte", (byte)PropertyType.Byte);
                        oType.Add(@"System.SByte", (byte)PropertyType.SByte);
                        oType.Add(@"System.Char", (byte)PropertyType.Char);
                        oType.Add(@"System.Version", (byte)PropertyType.Version);
                        oType.Add(@"System.DateTime", (byte)PropertyType.DateTime);
                        oType.Add(@"System.Guid", (byte)PropertyType.Guid);
                        oType.Add(@"System.Single", (byte)PropertyType.Single);
                        oType.Add(@"System.Net.IPAddress", (byte)PropertyType.IPAddress);
                        oType.Add(@"System.Byte[]", (byte)PropertyType.ByteArray);
                        oType.Add(@"System.Char[]", (byte)PropertyType.CharArray);
                        oType.Add(@"System.ByteArray", (byte)PropertyType.ByteArray);
                        oType.Add(@"System.CharArray", (byte)PropertyType.CharArray);
                        oType.Add(@"System.Uri", (byte)PropertyType.Uri);
                        oType.Add(@"System.Collections.Generic.Dictionary", (byte)PropertyType.Dictionary);

                        objectType = oType;
                    }
                }
			}
		}

        private static byte GetObjectType(object o)
		{
			byte objType;

			if(objectType.TryGetValue(o.GetType().FullName, out objType) == true)
				return objType;

			if(string.Compare(o.GetType().FullName, @"System.Array", true) == 0)
			{
				Type[] arrayTypes = Type.GetTypeArray((object[])o);

				if(arrayTypes[0] == Type.GetType("System.Byte"))
				{
					if(objectType.TryGetValue(@"System.ByteArray", out objType) == true)
						return objType;
				}
				else
				{
					if(arrayTypes[0] == Type.GetType("System.Char"))
					{
						if(objectType.TryGetValue(@"System.CharArray", out objType) == true)
							return objType;
					}
				}
			}

            if (o.GetType().FullName.StartsWith(@"System.Collections.Generic.Dictionary") == true)
                return objectType[@"System.Collections.Generic.Dictionary"];

            ResourceManager rm = new ResourceManager("WspEvent.WspEvent", Assembly.GetExecutingAssembly());

            throw new EventTypeNotSupportedException(rm.GetString("UnknownType"));
		}
	}
}
