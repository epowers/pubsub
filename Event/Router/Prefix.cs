using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Diagnostics;
using System.ServiceProcess;
using System.Threading;
using System.Text;
using System.Reflection;
using System.Reflection.Emit;
using System.Xml.XPath;

namespace Microsoft.WebSolutionsPlatform.Event
{
	public partial class Router : ServiceBase
	{
		internal class PrefixStream : Stream
		{
			internal int prefixLength;

			internal byte[] prefixBuffer;
			internal BinaryReader prefixReader;

            internal PrefixStream()
            {
                prefixLength = 100;
                prefixBuffer = new byte[prefixLength];
                prefixReader = new BinaryReader(this, Encoding.UTF8);
            }

            public override bool CanRead
			{
				get
				{
					return true;
				}
			}

            public override bool CanSeek
			{
				get
				{
					return false;
				}
			}

            public override bool CanWrite
			{
				get
				{
					return false;
				}
			}

            public override long Length
			{
				get
				{
					return (long)prefixBuffer.Length;
				}
			}

			private long position;
            public override long Position
			{
				get
				{
					return position;
				}
				set
				{
					position = value;
				}
			}

            public override void Flush()
			{
			}

            public override long Seek(long offset, SeekOrigin origin)
			{
				return 0;
			}

            public override void SetLength(long length)
			{
			}

            public override int Read(byte[] buffer, int offset, int count)
			{
				Buffer.BlockCopy(prefixBuffer, (int)Position, buffer, offset, count);

                Position = Position + count;

				return count;
			}

            public override void Write(byte[] buffer, int offset, int count)
			{
			}
		}
	}
}
