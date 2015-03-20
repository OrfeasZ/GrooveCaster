using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace GrooveCaster.Managers
{
    public static class StatisticsManager
    {
        private static Timer m_ListenerTimer;

        private static Timer m_StatsStorageTimer;

        private static Int64 m_CurrentListeners;

        static StatisticsManager()
        {
            m_CurrentListeners = 0;
        }

        internal static void Init()
        {
            if (m_ListenerTimer != null)
                m_ListenerTimer.Dispose();

            if (m_StatsStorageTimer != null)
                m_StatsStorageTimer.Dispose();

            // Update our local listener count every 30 sec.
            m_ListenerTimer = new Timer()
            {
                Interval = 30000,
                AutoReset = true
            };

            m_ListenerTimer.Elapsed += UpdateListenerCount;
            m_ListenerTimer.Start();

            // Store stats every minute, on the minute.
            m_StatsStorageTimer = new Timer()
            {
                AutoReset = false,
                Interval = GetStoreInterval()
            };

            m_StatsStorageTimer.Elapsed += StoreStats;
            m_StatsStorageTimer.Start();
        }

        private static void StoreStats(object p_Sender, ElapsedEventArgs p_ElapsedEventArgs)
        {
            Console.WriteLine("Current Listeners: {0}", m_CurrentListeners);

            // Restart the storage timer.
            m_StatsStorageTimer.Interval = GetStoreInterval();
            m_StatsStorageTimer.Start();
        }

        private static void UpdateListenerCount(object p_Sender, ElapsedEventArgs p_ElapsedEventArgs)
        {
            Application.Library.Broadcast.GetListenerCount(p_Listeners => m_CurrentListeners = p_Listeners);
        }

        private static double GetStoreInterval()
        {
            var s_Now = DateTime.UtcNow;
            return ((s_Now.Second > 30 ? 120 : 60) - s_Now.Second) * 1000 - s_Now.Millisecond;
        }
    }
}
