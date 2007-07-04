using System;
using System.Runtime.Serialization;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using System.Resources;
using System.Reflection;

[assembly: CLSCompliant(true)]

namespace Microsoft.WebSolutionsPlatform.Common
{
    enum ReturnCode : int
    {
        Success = 0,
        Timeout = -1,
        QueueNameDoesNotExist = 2,
        InvalidArgument = 3,
        InsufficientSpace = 5,
        InsufficientMemory = 1455,
        Overflow = 9999
    }

    internal class NativeMethods
    {
        [DllImport("SharedMemoryMgr.dll", ExactSpelling = true, CharSet = CharSet.Ansi)]
        internal static extern ReturnCode InitMemoryMgr(
            String sharedMemoryName,
            UInt32 sharedMemorySize,
            ref IntPtr commBuffer);

        [DllImport("SharedMemoryMgr.dll", ExactSpelling = true)]
        internal static extern ReturnCode ReleaseMemoryMgr(ref IntPtr commBuffer);

        [DllImport("SharedMemoryMgr.dll", ExactSpelling = true, CharSet = CharSet.Ansi)]
        internal static extern ReturnCode JoinMemoryMgr(
            String sharedMemoryName,
            ref IntPtr commBuffer);

        [DllImport("SharedMemoryMgr.dll", ExactSpelling = true)]
        internal static extern UInt32 GetQueueSize(ref IntPtr commBuffer);

        [DllImport("SharedMemoryMgr.dll", ExactSpelling = true)]
        internal static extern ReturnCode PutBuffer(
            [Out] byte[] eventBuffer,
            UInt32 eventLength,
            UInt32 timeOut,
            ref IntPtr commBuffer);

        [DllImport("SharedMemoryMgr.dll", ExactSpelling = true)]
        internal static extern ReturnCode GetBuffer(
            [In, Out] byte[] eventBuffer,
            UInt32 eventBufferLength,
            UInt32 timeOut,
            ref UInt32 bytesRead,
            ref IntPtr commBuffer);

        private NativeMethods()
        {
        }
    }

    /// <summary>
    /// This is the class that encapsulates the underlying shared memory queue.
    /// </summary>
    public class SharedQueue : IDisposable
    {
        static private int headerSize = 30;  // This must be >= to the header size used by SharedMemoryMgr
        private byte[] buffer;
        private UInt32 bufferLength;
        private IntPtr commBuffer;

        private object lockObject = new object();

        private bool disposed;

        private UInt32 queueSize;
        /// <summary>
        /// Size in bytes of the SharedQueue
        /// </summary>
        [CLSCompliant(false)]
        public UInt32 QueueSize
        {
            get
            {
                if (queueSize == 0)
                {
                    try
                    {
                        queueSize = NativeMethods.GetQueueSize(ref commBuffer);
                    }
                    catch
                    {
                        // Do nothing
                    }
                }

                return queueSize;
            }
        }

        /// <summary>
        /// Base constructor to create a new SharedQueue.
        /// </summary>
        /// <param name="name">Name of the SharedQueue</param>
        /// <param name="size">Size in bytes of the queue</param>
        /// <param name="averageItemSize">Average expected size in bytes of items in queue</param>
        [CLSCompliant(false)]
        public SharedQueue(string name, UInt32 size, UInt32 averageItemSize)
        {
            if (size < 1000)
            {
                ResourceManager rm = new ResourceManager("WspSharedQueue.WspSharedQueue", Assembly.GetExecutingAssembly());

                throw new ArgumentOutOfRangeException("size", rm.GetString("MinValue"));
            }

            if (averageItemSize > UInt32.MaxValue)
            {
                ResourceManager rm = new ResourceManager("WspSharedQueue.WspSharedQueue", Assembly.GetExecutingAssembly());

                throw new ArgumentOutOfRangeException("averageItemSize", rm.GetString("MaxValue") + int.MaxValue.ToString());
            }

            if (averageItemSize > size)
            {
                ResourceManager rm = new ResourceManager("WspSharedQueue.WspSharedQueue", Assembly.GetExecutingAssembly());

                throw new ArgumentOutOfRangeException("averageItemSize", rm.GetString("NotExceedQueueSize"));
            }

            if (averageItemSize > (int.MaxValue - headerSize))
            {
                buffer = new byte[int.MaxValue];
            }
            else
            {
                buffer = new byte[averageItemSize + headerSize];
            }

            bufferLength = (UInt32)buffer.Length;

            ReturnCode rc = NativeMethods.InitMemoryMgr(name, size, ref commBuffer);

            if (rc != ReturnCode.Success)
            {
                if (rc == ReturnCode.InvalidArgument)
                {
                    ResourceManager rm = new ResourceManager("WspSharedQueue.WspSharedQueue", Assembly.GetExecutingAssembly());

                    throw new ArgumentException(rm.GetString("InvalidArgumentValue"), "name");
                }
                else
                {
                    if (rc == ReturnCode.InsufficientMemory)
                    {
                        ResourceManager rm = new ResourceManager("WspSharedQueue.WspSharedQueue", Assembly.GetExecutingAssembly());

                        throw new SharedQueueInsufficientMemoryException(rm.GetString("WrappedInitFailure"));
                    }
                    else
                    {
                        ResourceManager rm = new ResourceManager("WspSharedQueue.WspSharedQueue", Assembly.GetExecutingAssembly());

                        throw new SharedQueueInitializationException(rm.GetString("WrappedHresult") + rc.ToString() + rm.GetString("WrappedInitFailure"));
                    }
                }
            }
        }

        /// <summary>
        /// Base constructor to join an existing SharedQueue.
        /// </summary>
        /// <param name="name">Name of the SharedQueue</param>
        /// <param name="averageItemSize">Average expected size in bytes of items in queue</param>
        [CLSCompliant(false)]
        public SharedQueue(string name, UInt32 averageItemSize)
        {
            if (averageItemSize > UInt32.MaxValue)
            {
                ResourceManager rm = new ResourceManager("WspSharedQueue.WspSharedQueue", Assembly.GetExecutingAssembly());

                throw new ArgumentOutOfRangeException("averageItemSize", rm.GetString("MaxValue") + int.MaxValue.ToString());
            }

            if (averageItemSize > (int.MaxValue - headerSize))
            {
                buffer = new byte[int.MaxValue];
            }
            else
            {
                buffer = new byte[averageItemSize + headerSize];
            }

            bufferLength = (UInt32)buffer.Length;

            ReturnCode rc = NativeMethods.JoinMemoryMgr(name, ref commBuffer);

            if (rc != ReturnCode.Success)
            {
                if (rc == ReturnCode.InvalidArgument)
                {
                    ResourceManager rm = new ResourceManager("WspSharedQueue.WspSharedQueue", Assembly.GetExecutingAssembly());

                    throw new ArgumentException(rm.GetString("InvalidArgumentValue"), "name");
                }
                else
                {
                    if (rc == ReturnCode.QueueNameDoesNotExist)
                    {
                        ResourceManager rm = new ResourceManager("WspSharedQueue.WspSharedQueue", Assembly.GetExecutingAssembly());

                        throw new SharedQueueDoesNotExistException(rm.GetString("NameDoesNotExist"));
                    }
                    else
                    {
                        ResourceManager rm = new ResourceManager("WspSharedQueue.WspSharedQueue", Assembly.GetExecutingAssembly());

                        throw new SharedQueueInitializationException(rm.GetString("WrappedHresult") + rc.ToString() + rm.GetString("WrappedInitFailure"));
                    }
                }
            }

            if (averageItemSize > QueueSize)
            {
                try
                {
                    NativeMethods.ReleaseMemoryMgr(ref commBuffer);
                }
                finally
                {
                    ResourceManager rm = new ResourceManager("WspSharedQueue.WspSharedQueue", Assembly.GetExecutingAssembly());

                    throw new ArgumentOutOfRangeException("averageItemSize", rm.GetString("NotExceedQueueSize"));
                }
            }

        }

        /// <summary>
        /// Dispose the SharedQueue object to release the unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
        }

        /// <summary>
        /// Dispose the SharedQueue object to release the unmanaged resources.
        /// </summary>
        /// <param name="disposing"></param>
        protected virtual void Dispose(bool disposing) 
        {
            if (!disposed)
            {
                try
                {
                    NativeMethods.ReleaseMemoryMgr(ref commBuffer);
                }
                finally
                {
                    disposed = true;

                    if (disposing)
                    {
                        GC.SuppressFinalize(this);
                    }
                }
            }
        }

        /// <summary>
        /// Destructor for the class.
        /// </summary>
        ~SharedQueue()
        {
            Dispose(false);
        }

        /// <summary>
        /// Add an element to the queue.
        /// </summary>
        /// <param name="item">Item being added to the queue</param>
        /// <param name="timeout">Timeout for the operation</param>
        [CLSCompliant(false)]
        public void Enqueue(ref byte[] item, UInt32 timeout)
        {
            ReturnCode rc;

            if (item == null)
            {
                ResourceManager rm = new ResourceManager("WspSharedQueue.WspSharedQueue", Assembly.GetExecutingAssembly());

                throw new ArgumentException(rm.GetString("InvalidArgumentValue"), "item");
            }

            lock (lockObject)
            {
                rc = NativeMethods.PutBuffer(item, (uint)item.Length, timeout, ref commBuffer);
            }

            if (rc == ReturnCode.Success)
            {
                return;
            }

            if (rc == ReturnCode.Timeout)
            {
                ResourceManager rm = new ResourceManager("WspSharedQueue.WspSharedQueue", Assembly.GetExecutingAssembly());

                throw new TimeoutException(rm.GetString("EnqueueTimeout"));
            }

            if (rc == ReturnCode.InsufficientSpace)
            {
                ResourceManager rm = new ResourceManager("WspSharedQueue.WspSharedQueue", Assembly.GetExecutingAssembly());

                throw new SharedQueueFullException(rm.GetString("QueueFull"));
            }

            if (rc != ReturnCode.Success)
            {
                ResourceManager rm = new ResourceManager("WspSharedQueue.WspSharedQueue", Assembly.GetExecutingAssembly());

                throw new SharedQueueException(rm.GetString("WrappedHresult") + rc.ToString() + rm.GetString("WrappedEnqueueFailed"));
            }
        }

        /// <summary>
        /// Get an element from the queue.
        /// </summary>
        /// <param name="timeout">Timeout for the operation</param>
        [CLSCompliant(false)]
        public byte[] Dequeue(UInt32 timeout)
        {
            ReturnCode rc = ReturnCode.Timeout;
            UInt32 bytesRead = 0;
            byte[] returnBuffer;

            rc = NativeMethods.GetBuffer(buffer, bufferLength, timeout, ref bytesRead, ref commBuffer);

            if (rc == ReturnCode.Timeout)
            {
                return null;
            }

            if (rc == ReturnCode.Overflow)
            {
                buffer = new byte[bytesRead + headerSize];
                bufferLength = (UInt32)buffer.Length;

                rc = NativeMethods.GetBuffer(buffer, bufferLength, timeout, ref bytesRead, ref commBuffer);
            }

            if (rc != ReturnCode.Success)
            {
                ResourceManager rm = new ResourceManager("WspSharedQueue.WspSharedQueue", Assembly.GetExecutingAssembly());

                throw new SharedQueueException(rm.GetString("WrappedHresult") + rc.ToString() + rm.GetString("WrappedDequeueFailed"));
            }

            returnBuffer = new byte[bytesRead];

            Buffer.BlockCopy(buffer, 0, returnBuffer, 0, (int)bytesRead);

            return returnBuffer;
        }
    }
}
