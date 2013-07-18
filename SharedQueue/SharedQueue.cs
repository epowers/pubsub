using System;
using System.Runtime.Serialization;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using System.Runtime.ExceptionServices;
using System.Resources;
using System.Reflection;

[assembly: CLSCompliant(true)]

namespace Microsoft.WebSolutionsPlatform.Common
{
    /// <summary>
    /// The ReturnCode enum defines the return codes when publishing events
    /// </summary>
    public enum ReturnCode : int
    {
        /// <summary>
        /// Action was successful
        /// </summary>
        Success = 0,
        /// <summary>
        /// Action timed out
        /// </summary>
        Timeout = -1,
        /// <summary>
        /// Queue does not exist. Check to see if WspEventRouter is running.
        /// </summary>
        QueueNameDoesNotExist = 2,
        /// <summary>
        /// Invalid argument was passed
        /// </summary>
        InvalidArgument = 3,
        /// <summary>
        /// There was not sufficient space to publish the event. This indicates the queue is full.
        /// </summary>
        InsufficientSpace = 5,
        /// <summary>
        /// There is not sufficient memory to processing the event
        /// </summary>
        InsufficientMemory = 1455,
        /// <summary>
        /// The event overflowed the buffer
        /// </summary>
        Overflow = 9999
    }

    internal class NativeMethods
    {
        private class x86
        {
            private const string SharedMemoryMgr = "SharedMemoryMgrx86.dll";

            [DllImport(SharedMemoryMgr, ExactSpelling = true, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
            internal static extern ReturnCode InitMemoryMgr(
                String sharedMemoryName,
                UInt32 sharedMemorySize,
                ref IntPtr commBuffer);

            [DllImport(SharedMemoryMgr, ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
            internal static extern ReturnCode ReleaseMemoryMgr(ref IntPtr commBuffer);

            [DllImport(SharedMemoryMgr, ExactSpelling = true, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
            internal static extern ReturnCode JoinMemoryMgr(
                String sharedMemoryName,
                ref IntPtr commBuffer);

            [DllImport(SharedMemoryMgr, ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
            internal static extern UInt32 GetQueueSize(ref IntPtr commBuffer);

            [DllImport(SharedMemoryMgr, ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
            internal static extern ReturnCode PutBuffer(
                [Out] byte[] eventBuffer,
                UInt32 eventLength,
                UInt32 timeOut,
                ref IntPtr commBuffer);

            [DllImport(SharedMemoryMgr, ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
            internal static extern ReturnCode GetBuffer(
                [In, Out] byte[] eventBuffer,
                UInt32 eventBufferLength,
                UInt32 timeOut,
                ref UInt32 bytesRead,
                ref IntPtr commBuffer);
        }

        private class x64
        {
            private const string SharedMemoryMgr = "SharedMemoryMgrx64.dll";

            [DllImport(SharedMemoryMgr, ExactSpelling = true, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
            internal static extern ReturnCode InitMemoryMgr(
                String sharedMemoryName,
                UInt32 sharedMemorySize,
                ref IntPtr commBuffer);

            [DllImport(SharedMemoryMgr, ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
            internal static extern ReturnCode ReleaseMemoryMgr(ref IntPtr commBuffer);

            [DllImport(SharedMemoryMgr, ExactSpelling = true, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
            internal static extern ReturnCode JoinMemoryMgr(
                String sharedMemoryName,
                ref IntPtr commBuffer);

            [DllImport(SharedMemoryMgr, ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
            internal static extern UInt32 GetQueueSize(ref IntPtr commBuffer);

            [DllImport(SharedMemoryMgr, ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
            internal static extern ReturnCode PutBuffer(
                [Out] byte[] eventBuffer,
                UInt32 eventLength,
                UInt32 timeOut,
                ref IntPtr commBuffer);

            [DllImport(SharedMemoryMgr, ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
            internal static extern ReturnCode GetBuffer(
                [In, Out] byte[] eventBuffer,
                UInt32 eventBufferLength,
                UInt32 timeOut,
                ref UInt32 bytesRead,
                ref IntPtr commBuffer);
        }

        internal delegate ReturnCode InitMemoryMgrDelegate(
            String sharedMemoryName,
            UInt32 sharedMemorySize,
            ref IntPtr commBuffer);
        internal delegate ReturnCode ReleaseMemoryMgrDelegate(ref IntPtr commBuffer);
        internal delegate ReturnCode JoinMemoryMgrDelegate(
            String sharedMemoryName,
            ref IntPtr commBuffer);
        internal delegate UInt32 GetQueueSizeDelegate(ref IntPtr commBuffer);
        internal delegate ReturnCode PutBufferDelegate(
            [Out] byte[] eventBuffer,
            UInt32 eventLength,
            UInt32 timeOut,
            ref IntPtr commBuffer);
        internal delegate ReturnCode GetBufferDelegate(
            [In, Out] byte[] eventBuffer,
            UInt32 eventBufferLength,
            UInt32 timeOut,
            ref UInt32 bytesRead,
            ref IntPtr commBuffer);

        internal static InitMemoryMgrDelegate InitMemoryMgr;
        internal static ReleaseMemoryMgrDelegate ReleaseMemoryMgr;
        internal static JoinMemoryMgrDelegate JoinMemoryMgr;
        internal static GetQueueSizeDelegate GetQueueSize;
        internal static PutBufferDelegate PutBuffer;
        internal static GetBufferDelegate GetBuffer;

        static NativeMethods()
        {
            if (System.IntPtr.Size == 8)
            {
                InitMemoryMgr = x64.InitMemoryMgr;
                ReleaseMemoryMgr = x64.ReleaseMemoryMgr;
                JoinMemoryMgr = x64.JoinMemoryMgr;
                GetQueueSize = x64.GetQueueSize;
                PutBuffer = x64.PutBuffer;
                GetBuffer = x64.GetBuffer;
            }
            else
            {
                InitMemoryMgr = x86.InitMemoryMgr;
                ReleaseMemoryMgr = x86.ReleaseMemoryMgr;
                JoinMemoryMgr = x86.JoinMemoryMgr;
                GetQueueSize = x86.GetQueueSize;
                PutBuffer = x86.PutBuffer;
                GetBuffer = x86.GetBuffer;
            }
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
                throw new ArgumentOutOfRangeException("size", "Minimum value is 1000");
            }

            if (averageItemSize > (Int32.MaxValue - headerSize))
            {
                throw new ArgumentOutOfRangeException("averageItemSize", "Maximum value is " + (Int32.MaxValue - headerSize).ToString());
            }

            if (averageItemSize > (size - headerSize))
            {
                throw new ArgumentOutOfRangeException("averageItemSize", "Value must not exceed the queue size");
            }

            if (averageItemSize > (UInt32)(int.MaxValue - headerSize))
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
                    throw new ArgumentException("Invalid argument value", "name");
                }
                else
                {
                    if (rc == ReturnCode.InsufficientMemory)
                    {
                        throw new SharedQueueInsufficientMemoryException("(HRESULT:" + rc.ToString() + ") Insufficient memory to allocate the event queue");
                    }
                    else
                    {
                        throw new SharedQueueInitializationException("(HRESULT:" + rc.ToString() + ") SharedQueue failed to initialize");
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
                throw new ArgumentOutOfRangeException("averageItemSize", "Maximum value is" + int.MaxValue.ToString());
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
                    throw new ArgumentException("Invalid argument value", "name");
                }
                else
                {
                    if (rc == ReturnCode.QueueNameDoesNotExist)
                    {
                        throw new SharedQueueDoesNotExistException("Queue name does not exist");
                    }
                    else
                    {
                        throw new SharedQueueInitializationException("(HRESULT:" + rc.ToString() + ") SharedQueue failed to initialize");
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
                    throw new ArgumentOutOfRangeException("averageItemSize", "Value must not exceed the queue size");
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
        public void Enqueue(byte[] item, UInt32 timeout)
        {
            ReturnCode rc;

            Enqueue(item, out rc, timeout);

            if (rc == ReturnCode.Success)
            {
                return;
            }

            if (rc == ReturnCode.Timeout)
            {
                throw new TimeoutException("Enqueue timed out");
            }

            if (rc == ReturnCode.InsufficientSpace)
            {
                throw new SharedQueueFullException("Queue is full");
            }

            if (rc != ReturnCode.Success)
            {
                throw new SharedQueueException("(HRESULT:" + rc.ToString() + ") Enqueue failed");
            }
        }

        /// <summary>
        /// Add an element to the queue.
        /// </summary>
        /// <param name="item">Item being added to the queue</param>
        /// <param name="rc">Return code</param>
        /// <param name="timeout">Timeout for the operation</param>
        [CLSCompliant(false)]
        [HandleProcessCorruptedStateExceptions]
        public void Enqueue(byte[] item, out ReturnCode rc, UInt32 timeout)
        {
            try
            {
                if (item == null)
                {
                    throw new ArgumentException("Invalid argument value", "item");
                }

                lock (lockObject)
                {
                    rc = NativeMethods.PutBuffer(item, (uint)item.Length, timeout, ref commBuffer);
                }
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        /// <summary>
        /// Get an element from the queue.
        /// </summary>
        /// <param name="timeout">Timeout for the operation</param>
        [CLSCompliant(false)]
        [HandleProcessCorruptedStateExceptions]
        public byte[] Dequeue(UInt32 timeout)
        {
            ReturnCode rc = ReturnCode.Timeout;
            UInt32 bytesRead = 0;
            byte[] returnBuffer;

            try
            {
                rc = NativeMethods.GetBuffer(buffer, bufferLength, timeout, ref bytesRead, ref commBuffer);

                if (rc == ReturnCode.Timeout || bytesRead == 0)
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
                    throw new SharedQueueException("(HRESULT:" + rc.ToString() + ") Dequeue failed");
                }

                returnBuffer = new byte[bytesRead];

                Buffer.BlockCopy(buffer, 0, returnBuffer, 0, (int)bytesRead);

                return returnBuffer;
            }
            catch (Exception)
            {
                throw;
            }
        }
    }
}
