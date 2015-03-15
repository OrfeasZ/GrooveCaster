using System;
using System.Collections.Generic;
using GrooveCasterServer.Models;
using GS.Lib.Enums;
using GS.Lib.Events;
using GS.Lib.Models;
using ServiceStack.OrmLite;

namespace GrooveCasterServer.Managers
{
    public static class BroadcastManager
    {
        public static bool CreatingBroadcast { get; set; }

        static BroadcastManager()
        {
            CreatingBroadcast = false;
        }

        public static void Init()
        {
            CreatingBroadcast = false;
            Program.Library.RegisterEventHandler(ClientEvent.BroadcastCreated, OnBroadcastCreated);
            Program.Library.RegisterEventHandler(ClientEvent.BroadcastCreationFailed, OnBroadcastCreationFailed);
        }

        public static void CreateBroadcast()
        {
            if (CreatingBroadcast)
                return;

            if (QueueManager.CollectionSongs.Count < 2)
                return;

            CreatingBroadcast = true;

            Program.Library.Broadcast.CreateBroadcast(GetBroadcastName(), GetBroadcastDescription(), GetBroadcastCategoryTag());
        }

        public static String GetBroadcastName()
        {
            using (var s_Db = Program.DbConnectionString.OpenDbConnection())
            {
                // TODO: Variable substitution.
                var s_Setting = s_Db.SingleById<CoreSetting>("bcname");
                return s_Setting == null ? "" : s_Setting.Value;
            }
        }

        public static String GetBroadcastDescription()
        {
            using (var s_Db = Program.DbConnectionString.OpenDbConnection())
            {
                // TODO: Variable substitution.
                var s_Setting = s_Db.SingleById<CoreSetting>("bcdesc");
                return s_Setting == null ? "" : s_Setting.Value;
            }
        }

        public static CategoryTag GetBroadcastCategoryTag()
        {
            using (var s_Db = Program.DbConnectionString.OpenDbConnection())
            {
                var s_Setting = s_Db.SingleById<CoreSetting>("bctag");

                if (s_Setting == null)
                    return new CategoryTag();

                var s_Tag = s_Setting.Value.Split(':');
                return new CategoryTag(s_Tag[0], s_Tag[1]);
            }
        }

        private static void OnBroadcastCreated(SharkEvent p_SharkEvent)
        {
            CreatingBroadcast = false;

            QueueManager.FetchCollectionSongs();
            QueueManager.ClearHistory();

            // Disable mobile compliance if needed.
            if (!SettingsManager.MobileCompliance())
                DisableMobileCompliance();

            // Add two random songs to the collection.
            var s_Random = new Random();
            var s_FirstSongIndex = s_Random.Next(0, QueueManager.CollectionSongs.Count);
            var s_SecondSongIndex = s_Random.Next(0, QueueManager.CollectionSongs.Count);

            var s_FirstSong = QueueManager.CollectionSongs[s_FirstSongIndex];
            var s_SecondSong = QueueManager.CollectionSongs[s_SecondSongIndex];

            while (s_SecondSong == s_FirstSong)
            {
                s_SecondSongIndex = s_Random.Next(0, QueueManager.CollectionSongs.Count);
                s_SecondSong = QueueManager.CollectionSongs[s_SecondSongIndex];
            }

            var s_QueueIDs = Program.Library.Broadcast.AddSongs(new List<Int64> { s_FirstSong, s_SecondSong });
            Program.Library.Broadcast.PlaySong(s_FirstSong, s_QueueIDs[s_FirstSong]);
        }

        public static void DisableMobileCompliance()
        {
            Program.Library.Broadcast.DisableMobileCompliance();
        }

        private static void OnBroadcastCreationFailed(SharkEvent p_SharkEvent)
        {
            CreatingBroadcast = false;
        }
    }
}
