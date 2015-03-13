using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
                return s_Db.SingleById<CoreSetting>("bcname").Value;
            }
        }

        public static String GetBroadcastDescription()
        {
            using (var s_Db = Program.DbConnectionString.OpenDbConnection())
            {
                // TODO: Variable substitution.
                return s_Db.SingleById<CoreSetting>("bcdesc").Value;
            }
        }

        public static CategoryTag GetBroadcastCategoryTag()
        {
            using (var s_Db = Program.DbConnectionString.OpenDbConnection())
            {
                var s_Tag = s_Db.SingleById<CoreSetting>("bctag").Value.Split(':');
                return new CategoryTag(s_Tag[0], s_Tag[1]);
            }
        }

        private static void OnBroadcastCreated(SharkEvent p_SharkEvent)
        {
            CreatingBroadcast = false;

            QueueManager.FetchCollectionSongs();
            QueueManager.ClearHistory();

            // Add two random song to the collection.
            var s_Random = new Random();
            var s_FirstSongIndex = s_Random.Next(0, QueueManager.CollectionSongs.Count - 1);
            var s_SecondSongIndex = s_Random.Next(0, QueueManager.CollectionSongs.Count - 1);

            var s_FirstSong = QueueManager.CollectionSongs[s_FirstSongIndex];
            var s_SecondSong = QueueManager.CollectionSongs[s_SecondSongIndex];

            while (s_SecondSong == s_FirstSong)
            {
                s_SecondSongIndex = s_Random.Next(0, QueueManager.CollectionSongs.Count - 1);
                s_SecondSong = QueueManager.CollectionSongs[s_SecondSongIndex];
            }

            Program.Library.Broadcast.AddSongs(new List<Int64> { s_FirstSong, s_SecondSong });
        }

        private static void OnBroadcastCreationFailed(SharkEvent p_SharkEvent)
        {
            CreatingBroadcast = false;
        }
    }
}
