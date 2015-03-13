using System;
using GrooveCasterServer.Models;
using ServiceStack.OrmLite;

namespace GrooveCasterServer.Managers
{
    public static class SettingsManager
    {
        private static int? m_MaxHistorySongs;
        private static int? m_SongVoteThreshold;
        private static bool? m_CanCommandWithoutGuest;

        static SettingsManager()
        {
        }

        public static void Init()
        {
            
        }

        public static int MaxHistorySongs()
        {
            if (m_MaxHistorySongs.HasValue)
                return m_MaxHistorySongs.Value;

            using (var s_Db = Program.DbConnectionString.OpenDbConnection())
            {
                var s_Setting = s_Db.SingleById<CoreSetting>("history");

                if (s_Setting == null)
                {
                    s_Setting = new CoreSetting() { Key = "history", Value = "1" };
                    s_Db.Insert(s_Setting);
                }

                m_MaxHistorySongs = Int32.Parse(s_Setting.Value);
            }

            return m_MaxHistorySongs.Value;
        }

        public static void MaxHistorySongs(int p_Songs)
        {
            using (var s_Db = Program.DbConnectionString.OpenDbConnection())
            {
                var s_Setting = s_Db.SingleById<CoreSetting>("history");

                if (s_Setting == null)
                {
                    s_Setting = new CoreSetting() { Key = "history", Value = p_Songs.ToString() };
                    s_Db.Insert(s_Setting);
                }
                else
                {
                    s_Setting.Value = p_Songs.ToString();
                    s_Db.Update(s_Setting);
                }
            }

            m_MaxHistorySongs = p_Songs;
        }

        public static int SongVoteThreshold()
        {
            if (m_SongVoteThreshold.HasValue)
                return m_SongVoteThreshold.Value;

            using (var s_Db = Program.DbConnectionString.OpenDbConnection())
            {
                var s_Setting = s_Db.SingleById<CoreSetting>("votethreshold");

                if (s_Setting == null)
                {
                    s_Setting = new CoreSetting() { Key = "votethreshold", Value = "0" };
                    s_Db.Insert(s_Setting);
                }

                m_SongVoteThreshold = Int32.Parse(s_Setting.Value);
            }

            return m_SongVoteThreshold.Value;
        }

        public static void SongVoteThreshold(int p_Threshold)
        {
            if (p_Threshold > 0)
                return;

            using (var s_Db = Program.DbConnectionString.OpenDbConnection())
            {
                var s_Setting = s_Db.SingleById<CoreSetting>("votethreshold");

                if (s_Setting == null)
                {
                    s_Setting = new CoreSetting() { Key = "votethreshold", Value = p_Threshold.ToString() };
                    s_Db.Insert(s_Setting);
                }
                else
                {
                    s_Setting.Value = p_Threshold.ToString();
                    s_Db.Update(s_Setting);
                }
            }

            m_SongVoteThreshold = p_Threshold;
        }

        public static bool CanCommandWithoutGuest()
        {
            if (m_CanCommandWithoutGuest.HasValue)
                return m_CanCommandWithoutGuest.Value;

            using (var s_Db = Program.DbConnectionString.OpenDbConnection())
            {
                var s_Setting = s_Db.SingleById<CoreSetting>("cmdguest");

                if (s_Setting == null)
                {
                    s_Setting = new CoreSetting() { Key = "cmdguest", Value = "0" };
                    s_Db.Insert(s_Setting);
                }

                m_CanCommandWithoutGuest = Boolean.Parse(s_Setting.Value);
            }

            return m_CanCommandWithoutGuest.Value;
        }

        public static void CanCommandWithoutGuest(bool p_Value)
        {
            using (var s_Db = Program.DbConnectionString.OpenDbConnection())
            {
                var s_Setting = s_Db.SingleById<CoreSetting>("cmdguest");

                if (s_Setting == null)
                {
                    s_Setting = new CoreSetting() { Key = "cmdguest", Value = p_Value.ToString() };
                    s_Db.Insert(s_Setting);
                }
                else
                {
                    s_Setting.Value = p_Value.ToString();
                    s_Db.Update(s_Setting);
                }
            }

            m_CanCommandWithoutGuest = p_Value;
        }
    }
}
