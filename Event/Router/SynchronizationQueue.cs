using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Diagnostics;

namespace Microsoft.WebSolutionsPlatform.Event
{
    /// <summary>
    /// A wrapper class of the Queue class to make the Queue class thread safe.
    /// </summary>
    /// <typeparam name="T"></typeparam>
	public class SynchronizationQueue<T> : Queue<T>, IDisposable
	{
        private bool disposed;

        private PerformanceCounter performanceCounter;

        private Mutex mut;

		private ManualResetEvent resetEvent;

		private int timeout;
		/// <summary>
		/// Timeout for blocking calls, default is 10000
		/// </summary>
		public int Timeout
		{
			get
			{
				return timeout;
			}
			internal set
			{
				timeout = value;
			}
		}

		/// <summary>
		/// Adds an object to the end of the Generic Queue
		/// </summary>
		/// <param name="item">The object to add to the queue</param>
		public new void Enqueue( T item )
		{
			Enqueue(item, timeout);
		}

		/// <summary>
		/// Adds an object to the end of the Generic Queue
		/// </summary>
		/// <param name="item">The object to add to the queue</param>
        /// <param name="timeoutIn">Timeout in milliseconds for call</param>
		public void Enqueue( T item, int timeoutIn )
		{
            if (mut.WaitOne(timeoutIn, false) == true)
			{
				base.Enqueue(item);

				resetEvent.Set();

                if (performanceCounter != null)
                    performanceCounter.Increment();

				mut.ReleaseMutex();
			}
			else
			{
				throw (new TimeoutException());
			}
		}

		/// <summary>
		/// Removes an object from the Generic Queue
		/// </summary>
		public new T Dequeue()
		{
			return Dequeue(timeout);
		}

		/// <summary>
		/// Removes an object from the Generic Queue
		/// </summary>
        /// <param name="timeoutIn">Timeout in milliseconds for call</param>
		public T Dequeue( int timeoutIn )
		{
			T item;

            if (base.Count > 0 || resetEvent.WaitOne(timeoutIn, false) == true)
			{
                if (mut.WaitOne(timeoutIn, false) == true)
				{
					try
					{
						item = base.Dequeue();

                        if (performanceCounter != null)
                            performanceCounter.Decrement();
					}
					catch(InvalidOperationException)
					{
						resetEvent.Reset();
						mut.ReleaseMutex();
						throw;
					}

					resetEvent.Set();

					mut.ReleaseMutex();
				}
				else
				{
                    return default(T);
				}
			}

			else
			{
                return default(T);
            }

			return item;
		}

		/// <summary>
		/// Default constructor
		/// </summary>
		public SynchronizationQueue()
		{
			mut = new Mutex(false);
			resetEvent = new ManualResetEvent(false);
			timeout = 10000;
		}

		/// <summary>
		/// Default constructor
		/// </summary>
        /// <param name="performanceCounter">Performance counter showing size of queue.</param>
		public SynchronizationQueue( PerformanceCounter performanceCounter )
		{
			mut = new Mutex(false);
			resetEvent = new ManualResetEvent(false);
			timeout = 10000;
            this.performanceCounter = performanceCounter;
            this.performanceCounter.RawValue = 0;
		}

        /// <summary>
        /// Default constructor
        /// </summary>
        /// <param name="capacity">The initial number of elements that the Generic Queue can contain</param>
        public SynchronizationQueue(int capacity)
            : base(capacity)
        {
            mut = new Mutex(false);
            resetEvent = new ManualResetEvent(false);
            timeout = 10000;
        }

        /// <summary>
        /// Default constructor
        /// </summary>
        /// <param name="capacity">The initial number of elements that the Generic Queue can contain</param>
        /// <param name="performanceCounter">Performance counter showing size of queue.</param>
        public SynchronizationQueue(int capacity, PerformanceCounter performanceCounter)
            : base(capacity)
        {
            mut = new Mutex(false);
            resetEvent = new ManualResetEvent(false);
            timeout = 10000;
            this.performanceCounter = performanceCounter;
            this.performanceCounter.RawValue = 0;
        }

		/// <summary>
		/// Default constructor
		/// </summary>
		/// <param name="collection">The collection whose elements are copied to the new Generic Queue</param>
		public SynchronizationQueue( IEnumerable<T> collection )
			: base(collection)
		{
			mut = new Mutex(false);
			resetEvent = new ManualResetEvent(false);
			timeout = 10000;
		}

        /// <summary>
        /// Default constructor
        /// </summary>
        /// <param name="collection">The collection whose elements are copied to the new Generic Queue</param>
        /// <param name="performanceCounter">Performance counter showing size of queue.</param>
        public SynchronizationQueue(IEnumerable<T> collection, PerformanceCounter performanceCounter)
            : base(collection)
        {
            mut = new Mutex(false);
            resetEvent = new ManualResetEvent(false);
            timeout = 10000;
            this.performanceCounter = performanceCounter;
            this.performanceCounter.RawValue = 0;
        }

		/// <summary>
		/// Disposes of the object.
		/// </summary>
		public void Dispose()
		{
            Dispose(true);
        }

        /// <summary>
        /// Disposes of the object.
        /// </summary>
        /// <param name="disposing">True if disposing.</param>
        protected virtual void Dispose(bool disposing) 
        {
            if (!disposed)
            {
                if (mut != null)
                {
                    mut.Close();
                    mut = null;

                    resetEvent.Close();
                    resetEvent = null;

                    if (performanceCounter != null)
                        performanceCounter.RawValue = 0;
                }

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
        ~SynchronizationQueue()
        {
            Dispose(false);
        }
	}
}
