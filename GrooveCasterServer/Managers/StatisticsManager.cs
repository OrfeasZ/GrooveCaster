using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using GrooveCaster.Models;
using ServiceStack.OrmLite;

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
            var s_Now = DateTime.UtcNow;
            var s_CurrentTime = new DateTime(s_Now.Year, s_Now.Month, s_Now.Day, s_Now.Hour, s_Now.Minute, 0);

            var s_ListenersUnit = new StatisticsUnit
            {
                Date = s_CurrentTime,
                IntegerValue = m_CurrentListeners,
                Key = "lsnr",
                Type = StatisticsUnit.UnitType.Integer
            };

            using (var s_Db = Database.GetStatsConnection())
                s_Db.Save(s_ListenersUnit);

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

        public static List<StatisticsUnit> GetUnits(String p_Key)
        {
            using (var s_Db = Database.GetStatsConnection())
                return s_Db.Select<StatisticsUnit>(p_Unit => p_Unit.Key == p_Key);
        }

        public static List<StatisticsUnit> GetUnits(String p_Key, DateTime p_From)
        {
            using (var s_Db = Database.GetStatsConnection())
                return s_Db.Select<StatisticsUnit>(p_Unit => p_Unit.Key == p_Key && p_Unit.Date >= p_From);
        }

        public static List<StatisticsUnit> GetUnits(String p_Key, DateTime p_From, DateTime p_To)
        {
            using (var s_Db = Database.GetStatsConnection())
                return s_Db.Select<StatisticsUnit>(p_Unit => p_Unit.Key == p_Key && p_Unit.Date >= p_From && p_Unit.Date <= p_To);
        }
    }
}
