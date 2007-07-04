using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Diagnostics;
using System.Threading;
using System.Text;
using System.Reflection;
using System.Reflection.Emit;
using System.Xml.XPath;

namespace Microsoft.WebSolutionsPlatform.Event
{
    /// <summary>
    /// Helper class for the Event System. Not to be publically used.
    /// </summary>
    public class PrefixStream : Stream
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

        /// <summary>
        /// Always true
        /// </summary>
        public override bool CanRead
        {
            get
            {
                return true;
            }
        }

        /// <summary>
        /// Always false
        /// </summary>
        public override bool CanSeek
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// Always false
        /// </summary>
        public override bool CanWrite
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// Length of the stream
        /// </summary>
        public override long Length
        {
            get
            {
                return (long)prefixBuffer.Length;
            }
        }

        private long position;
        /// <summary>
        /// Current position in the stream.
        /// </summary>
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

        /// <summary>
        /// Not implemented.
        /// </summary>
        public override void Flush()
        {
        }

        /// <summary>
        /// Not implemented. Always returns 0;
        /// </summary>
        /// <param name="offset"></param>
        /// <param name="origin"></param>
        /// <returns></returns>
        public override long Seek(long offset, SeekOrigin origin)
        {
            return 0;
        }

        /// <summary>
        /// Not implemented.
        /// </summary>
        /// <param name="value"></param>
        public override void SetLength(long value)
        {
        }

        /// <summary>
        /// Read a block of bytes from the stream.
        /// </summary>
        /// <param name="buffer">Destination buffer of bytes</param>
        /// <param name="offset">Starting position in destination buffer</param>
        /// <param name="count">Number of bytes to copy</param>
        /// <returns></returns>
        public override int Read(byte[] buffer, int offset, int count)
        {
            Buffer.BlockCopy(prefixBuffer, (int)Position, buffer, offset, count);

            Position = Position + count;

            return count;
        }

        /// <summary>
        /// Not implemented.
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="offset"></param>
        /// <param name="count"></param>
        public override void Write(byte[] buffer, int offset, int count)
        {
        }
    }
}
