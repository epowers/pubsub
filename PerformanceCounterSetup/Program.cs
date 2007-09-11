using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.Resources;
using System.Reflection;

namespace PerformanceCounterSetup
{
    class PerformanceCounterSetup
    {
        internal static string categoryName;
        internal static string communicationCategoryName;
        internal static string categoryHelp;
        internal static string communicationCategoryHelp;
        internal static string subscriptionQueueSizeName;
        internal static string rePublisherQueueSizeName;
        internal static string persisterQueueSizeName;
        internal static string forwarderQueueSizeName;
        internal static string subscriptionEntriesName;
        internal static string eventsProcessedName;
        internal static string eventsProcessedBytesName;
        internal static string baseInstance;

        internal static PerformanceCounter subscriptionQueueSize;
        internal static PerformanceCounter rePublisherQueueSize;
        internal static PerformanceCounter persisterQueueSize;
        internal static PerformanceCounter forwarderQueueSize;
        internal static PerformanceCounter subscriptionEntries;
        internal static PerformanceCounter eventsProcessed;
        internal static PerformanceCounter eventsProcessedBytes;

        static void Main(string[] args)
        {
            CounterCreationDataCollection CCDC;

            ResourceManager rm = new ResourceManager("PerformanceCounterSetup.WspEventRouter", Assembly.GetExecutingAssembly());

            categoryName = rm.GetString("CategoryName");
            communicationCategoryName = rm.GetString("CommunicationCategoryName");
            categoryHelp = rm.GetString("CategoryHelp");
            communicationCategoryHelp = rm.GetString("CommunicationCategoryHelp");
            subscriptionQueueSizeName = rm.GetString("SubscriptionQueueSizeName");
            rePublisherQueueSizeName = rm.GetString("RePublisherQueueSizeName");
            persisterQueueSizeName = rm.GetString("PersisterQueueSizeName");
            forwarderQueueSizeName = rm.GetString("ForwarderQueueSizeName");
            subscriptionEntriesName = rm.GetString("SubscriptionEntriesName");
            eventsProcessedName = rm.GetString("EventsProcessedName");
            eventsProcessedBytesName = rm.GetString("EventsProcessedBytesName");
            baseInstance = rm.GetString("BaseInstance");

            if (PerformanceCounterCategory.Exists(categoryName) == true)
            {
                PerformanceCounterCategory.Delete(categoryName);
            }

            if (EventLog.SourceExists("WspEventRouter") == true)
            {
                EventLog.DeleteEventSource("WspEventRouter");
            }

            if (args.Length > 0)
            {
                if (args[0] == @"/d" || args[0] == @"/D")
                    return;
            }

            EventLog.CreateEventSource("WspEventRouter", "System");

            CCDC = new CounterCreationDataCollection();

            CounterCreationData subscriptionQueueCounter = new CounterCreationData();
            subscriptionQueueCounter.CounterType = PerformanceCounterType.NumberOfItems32;
            subscriptionQueueCounter.CounterName = subscriptionQueueSizeName;
            CCDC.Add(subscriptionQueueCounter);

            CounterCreationData rePublisherQueueCounter = new CounterCreationData();
            rePublisherQueueCounter.CounterType = PerformanceCounterType.NumberOfItems32;
            rePublisherQueueCounter.CounterName = rePublisherQueueSizeName;
            CCDC.Add(rePublisherQueueCounter);

            CounterCreationData persisterQueueCounter = new CounterCreationData();
            persisterQueueCounter.CounterType = PerformanceCounterType.NumberOfItems32;
            persisterQueueCounter.CounterName = persisterQueueSizeName;
            CCDC.Add(persisterQueueCounter);

            CounterCreationData subscriptionEntriesCounter = new CounterCreationData();
            subscriptionEntriesCounter.CounterType = PerformanceCounterType.NumberOfItems32;
            subscriptionEntriesCounter.CounterName = subscriptionEntriesName;
            CCDC.Add(subscriptionEntriesCounter);

            CounterCreationData eventsProcessedCounter = new CounterCreationData();
            eventsProcessedCounter.CounterType = PerformanceCounterType.RateOfCountsPerSecond32;
            eventsProcessedCounter.CounterName = eventsProcessedName;
            CCDC.Add(eventsProcessedCounter);

            CounterCreationData eventsProcessedBytesCounter = new CounterCreationData();
            eventsProcessedBytesCounter.CounterType = PerformanceCounterType.RateOfCountsPerSecond64;
            eventsProcessedBytesCounter.CounterName = eventsProcessedBytesName;
            CCDC.Add(eventsProcessedBytesCounter);

            PerformanceCounterCategory.Create(categoryName, categoryHelp,
                PerformanceCounterCategoryType.SingleInstance, CCDC);

            subscriptionQueueSize = new PerformanceCounter(categoryName, subscriptionQueueSizeName, string.Empty, false);
            rePublisherQueueSize = new PerformanceCounter(categoryName, rePublisherQueueSizeName, string.Empty, false);
            persisterQueueSize = new PerformanceCounter(categoryName, persisterQueueSizeName, string.Empty, false);
            subscriptionEntries = new PerformanceCounter(categoryName, subscriptionEntriesName, string.Empty, false);
            eventsProcessed = new PerformanceCounter(categoryName, eventsProcessedName, string.Empty, false);
            eventsProcessedBytes = new PerformanceCounter(categoryName, eventsProcessedBytesName, string.Empty, false);

            if (PerformanceCounterCategory.Exists(communicationCategoryName) == false)
            {
                CCDC = new CounterCreationDataCollection();

                CounterCreationData forwarderQueueCounter = new CounterCreationData();
                forwarderQueueCounter.CounterType = PerformanceCounterType.NumberOfItems32;
                forwarderQueueCounter.CounterName = forwarderQueueSizeName;
                CCDC.Add(forwarderQueueCounter);

                PerformanceCounterCategory.Create(communicationCategoryName, communicationCategoryHelp,
                    PerformanceCounterCategoryType.MultiInstance, CCDC);
            }

            forwarderQueueSize = new PerformanceCounter(communicationCategoryName, forwarderQueueSizeName, baseInstance, false);

            subscriptionQueueSize.RawValue = 0;
            rePublisherQueueSize.RawValue = 0;
            persisterQueueSize.RawValue = 0;
            forwarderQueueSize.RawValue = 0;
            subscriptionEntries.RawValue = 0;
            eventsProcessed.RawValue = 0;
            eventsProcessedBytes.RawValue = 0;

        }
    }
}
