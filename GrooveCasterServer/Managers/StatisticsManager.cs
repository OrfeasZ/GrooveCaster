using System;
using System.Collections.Generic;
using System.Timers;
using GrooveCaster.Models;
using GS.Lib.Enums;
using GS.Lib.Events;
using Newtonsoft.Json;
using ServiceStack.OrmLite;

namespace GrooveCaster.Managers
{
    public static class StatisticsManager
    {
        private static Timer m_ListenerTimer;

        private static Timer m_StatsStorageTimer;

        private static Int64 m_CurrentListeners;

        private static Queue<SongVoteEvent> m_PendingSongVoteEvents;

        private static Queue<UserJoinedBroadcastEvent> m_PendingUserJoinEvents; 
        private static Queue<UserLeftBroadcastEvent> m_PendingUserLeaveEvents; 

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

            m_PendingSongVoteEvents = new Queue<SongVoteEvent>();
            m_PendingUserJoinEvents = new Queue<UserJoinedBroadcastEvent>();
            m_PendingUserLeaveEvents = new Queue<UserLeftBroadcastEvent>();

            Application.Library.RegisterEventHandler(ClientEvent.SongVote, OnSongVote);
            Application.Library.RegisterEventHandler(ClientEvent.UserJoinedBroadcast, OnUserJoinedBroadcast);
            Application.Library.RegisterEventHandler(ClientEvent.UserLeftBroadcast, OnUserLeftBroadcast);
        }

        private static void OnUserLeftBroadcast(SharkEvent p_SharkEvent)
        {
            lock (m_PendingUserLeaveEvents)
                m_PendingUserLeaveEvents.Enqueue((UserLeftBroadcastEvent) p_SharkEvent);
        }

        private static void OnUserJoinedBroadcast(SharkEvent p_SharkEvent)
        {
            lock (m_PendingUserJoinEvents)
                m_PendingUserJoinEvents.Enqueue((UserJoinedBroadcastEvent) p_SharkEvent);
        }

        private static void OnSongVote(SharkEvent p_SharkEvent)
        {
            lock (m_PendingSongVoteEvents)
                m_PendingSongVoteEvents.Enqueue((SongVoteEvent) p_SharkEvent);
        }

        private static void StoreStats(object p_Sender, ElapsedEventArgs p_ElapsedEventArgs)
        {
            var s_Now = DateTime.UtcNow;
            var s_CurrentTime = new DateTime(s_Now.Year, s_Now.Month, s_Now.Day, s_Now.Hour, s_Now.Minute, 0);

            var s_Units = new List<StatisticsUnit>();

            s_Units.Add(new StatisticsUnit
            {
                Date = s_CurrentTime,
                IntegerValue = m_CurrentListeners,
                Key = "lsnr",
                Type = StatisticsUnit.UnitType.Integer
            });

            lock (m_PendingSongVoteEvents)
            {
                var s_VoteUnits = new List<SongVoteUnit>();
                while (m_PendingSongVoteEvents.Count > 0)
                {
                    var s_Event = m_PendingSongVoteEvents.Dequeue();

                    s_VoteUnits.Add(new SongVoteUnit()
                    {
                        SongID = Application.Library.Queue.GetSongIDForQueueID(s_Event.QueueSongID),
                        UserID = s_Event.UserID,
                        Vote = s_Event.VoteChange
                    });
                }

                if (s_VoteUnits.Count > 0)
                {
                    s_Units.Add(new StatisticsUnit
                    {
                        Date = s_CurrentTime,
                        StringValue = JsonConvert.SerializeObject(s_VoteUnits),
                        Key = "svot",
                        Type = StatisticsUnit.UnitType.String
                    });
                }
            }

            lock (m_PendingUserJoinEvents)
            {
                var s_JoinUnits = new List<Int64>();

                while (m_PendingUserJoinEvents.Count > 0)
                {
                    var s_Event = m_PendingUserJoinEvents.Dequeue();
                    s_JoinUnits.Add(s_Event.UserID);
                }

                if (s_JoinUnits.Count > 0)
                {
                    s_Units.Add(new StatisticsUnit
                    {
                        Date = s_CurrentTime,
                        StringValue = JsonConvert.SerializeObject(s_JoinUnits),
                        Key = "join",
                        Type = StatisticsUnit.UnitType.String
                    });
                }
            }

            lock (m_PendingUserLeaveEvents)
            {
                var s_LeaveUnits = new List<Int64>();

                while (m_PendingUserLeaveEvents.Count > 0)
                {
                    var s_Event = m_PendingUserLeaveEvents.Dequeue();
                    s_LeaveUnits.Add(s_Event.UserID);
                }

                if (s_LeaveUnits.Count > 0)
                {
                    s_Units.Add(new StatisticsUnit
                    {
                        Date = s_CurrentTime,
                        StringValue = JsonConvert.SerializeObject(s_LeaveUnits),
                        Key = "leav",
                        Type = StatisticsUnit.UnitType.String
                    });
                }
            }

            using (var s_Db = Database.GetStatsConnection())
                s_Db.SaveAll(s_Units);

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
