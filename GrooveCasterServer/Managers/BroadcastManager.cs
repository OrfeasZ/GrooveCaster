using System;
using System.Collections.Generic;
using System.Linq;
using GrooveCaster.Models;
using GS.Lib.Enums;
using GS.Lib.Events;
using GS.Lib.Models;
using ServiceStack.OrmLite;

namespace GrooveCaster.Managers
{
    public static class BroadcastManager
    {
        public static bool CreatingBroadcast { get; set; }

        static BroadcastManager()
        {
            CreatingBroadcast = false;
        }

        internal static void Init()
        {
            CreatingBroadcast = false;
            Application.Library.RegisterEventHandler(ClientEvent.BroadcastCreated, OnBroadcastCreated);
            Application.Library.RegisterEventHandler(ClientEvent.BroadcastCreationFailed, OnBroadcastCreationFailed);
            Application.Library.RegisterEventHandler(ClientEvent.ComplianceIssue, OnComplianceIssue);
            Application.Library.RegisterEventHandler(ClientEvent.PendingDestruction, OnPendingDestruction);
            Application.Library.RegisterEventHandler(ClientEvent.PlaybackStatusUpdate, OnPlaybackStatusUpdate);
        }

        private static void OnPlaybackStatusUpdate(SharkEvent p_SharkEvent)
        {
            if (Application.Library.Broadcast.CurrentBroadcastStatus != BroadcastStatus.Broadcasting)
                return;

            var s_Event = (PlaybackStatusUpdateEvent) p_SharkEvent;

            if (s_Event.Status != PlaybackStatus.Completed && s_Event.Status != PlaybackStatus.Paused &&
                s_Event.Status != PlaybackStatus.Suspended)
                return;

            var s_CurrentIndex = QueueManager.GetPlayingSongIndex();

            if (s_CurrentIndex != -1 && s_CurrentIndex < QueueManager.GetCurrentQueue().Count - 2)
            {
                var s_NextSong = QueueManager.GetCurrentQueue()[s_CurrentIndex + 1];
                Application.Library.Broadcast.PlaySong(s_NextSong.SongID, s_NextSong.QueueID);
                return;
            }

            // Allows for custom queuing logic by Modules.
            var s_ModuleSongs = ModuleManager.OnFetchingNextSongs(1);

            if (s_ModuleSongs != null && s_ModuleSongs.Count > 0)
            {
                var s_First = s_ModuleSongs.First();
                Application.Library.Broadcast.PlaySong(s_First.Key, s_First.Value);
                return;
            }

            // Try to play a new song.
            if (PlaylistManager.PlaylistActive && PlaylistManager.HasNextSong())
            {
                var s_SongID = PlaylistManager.DequeueNextSong();
                Application.Library.Broadcast.PlaySong(s_SongID, Application.Library.Broadcast.AddSongs(new List<Int64> { s_SongID })[s_SongID]);
                return;
            }

            var s_Random = new Random();
            var s_SongIndex = s_Random.Next(0, QueueManager.CollectionSongs.Count);
            var s_FirstSong = QueueManager.CollectionSongs[s_SongIndex];

            var s_QueueIDs = Application.Library.Broadcast.AddSongs(new List<Int64> { s_FirstSong });

            Application.Library.Broadcast.PlaySong(s_FirstSong, s_QueueIDs[s_FirstSong]);
        }

        private static void OnPendingDestruction(SharkEvent p_SharkEvent)
        {
            if (Application.Library.Broadcast.CurrentBroadcastStatus != BroadcastStatus.Broadcasting)
                return;

            // Allows for custom queuing logic by Modules.
            var s_ModuleSongs = ModuleManager.OnFetchingNextSongs(1);

            if (s_ModuleSongs != null && s_ModuleSongs.Count > 0)
            {
                var s_First = s_ModuleSongs.First();
                Application.Library.Broadcast.PlaySong(s_First.Key, s_First.Value);
                return;
            }

            if (PlaylistManager.PlaylistActive && PlaylistManager.HasNextSong())
            {
                var s_SongID = PlaylistManager.DequeueNextSong();
                Application.Library.Broadcast.PlaySong(s_SongID,  Application.Library.Broadcast.AddSongs(new List<Int64> { s_SongID })[s_SongID]);
                return;
            }

            // Broadcast ran out of songs somehow; add and play a random song.
            var s_Random = new Random();
            var s_SongIndex = s_Random.Next(0, QueueManager.CollectionSongs.Count);
            var s_FirstSong = QueueManager.CollectionSongs[s_SongIndex];

            var s_QueueIDs = Application.Library.Broadcast.AddSongs(new List<Int64> { s_FirstSong });

            Application.Library.Broadcast.PlaySong(s_FirstSong, s_QueueIDs[s_FirstSong]);
        }

        private static void OnComplianceIssue(SharkEvent p_SharkEvent)
        {
            if (!SettingsManager.MobileCompliance())
            {
                DisableMobileCompliance();
                return;
            }

            // Allows for custom queuing logic by Modules.
            var s_ModuleSongs = ModuleManager.OnFetchingNextSongs(1);

            if (s_ModuleSongs != null && s_ModuleSongs.Count > 0)
            {
                var s_First = s_ModuleSongs.First();
                Application.Library.Broadcast.PlaySong(s_First.Key, s_First.Value);
                return;
            }

            // Try to play a new song.
            if (PlaylistManager.PlaylistActive && PlaylistManager.HasNextSong())
            {
                var s_SongID = PlaylistManager.DequeueNextSong();
                Application.Library.Broadcast.PlaySong(s_SongID, Application.Library.Broadcast.AddSongs(new List<Int64> { s_SongID })[s_SongID]);
                return;
            }

            var s_Random = new Random();
            var s_SongIndex = s_Random.Next(0, QueueManager.CollectionSongs.Count);
            var s_FirstSong = QueueManager.CollectionSongs[s_SongIndex];

            var s_QueueIDs = Application.Library.Broadcast.AddSongs(new List<Int64> { s_FirstSong });

            Application.Library.Broadcast.PlaySong(s_FirstSong, s_QueueIDs[s_FirstSong]);
        }

        internal static void CreateBroadcast()
        {
            if (CreatingBroadcast)
                return;

            if (QueueManager.CollectionSongs.Count < 2)
                return;

            CreatingBroadcast = true;

            Application.Library.Broadcast.CreateBroadcast(GetBroadcastName(), GetBroadcastDescription(), GetBroadcastCategoryTag());
        }

        public static String GetBroadcastName()
        {
            using (var s_Db = Database.GetConnection())
            {
                // TODO: Variable substitution.
                var s_Setting = s_Db.SingleById<CoreSetting>("bcname");
                return s_Setting == null ? "" : s_Setting.Value;
            }
        }

        public static String GetBroadcastDescription()
        {
            using (var s_Db = Database.GetConnection())
            {
                // TODO: Variable substitution.
                var s_Setting = s_Db.SingleById<CoreSetting>("bcdesc");
                return s_Setting == null ? "" : s_Setting.Value;
            }
        }

        public static CategoryTag GetBroadcastCategoryTag()
        {
            using (var s_Db = Database.GetConnection())
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

            // Add two random songs to the collection.
            if (Application.Library.Queue.CurrentQueue.Count < 2)
            {
                var s_ModuleSongs = ModuleManager.OnFetchingNextSongs(2);

                if (s_ModuleSongs != null && s_ModuleSongs.Count > 0)
                {
                    var s_First = s_ModuleSongs.First();
                    Application.Library.Broadcast.PlaySong(s_First.Key, s_First.Value);
                    return;
                }

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

                var s_QueueIDs = Application.Library.Broadcast.AddSongs(new List<Int64> {s_FirstSong, s_SecondSong});

                if (Application.Library.Broadcast.PlayingSongID == 0)
                    Application.Library.Broadcast.PlaySong(s_FirstSong, s_QueueIDs[s_FirstSong]);
            }
            else if (Application.Library.Broadcast.PlayingSongID == 0)
            {
                var s_ModuleSongs = ModuleManager.OnFetchingNextSongs(1);

                if (s_ModuleSongs != null && s_ModuleSongs.Count > 0)
                {
                    var s_First = s_ModuleSongs.First();
                    Application.Library.Broadcast.PlaySong(s_First.Key, s_First.Value);
                    return;
                }

                var s_Random = new Random();
                var s_SongIndex = s_Random.Next(0, QueueManager.CollectionSongs.Count);
                var s_FirstSong = QueueManager.CollectionSongs[s_SongIndex];

                var s_QueueIDs = Application.Library.Broadcast.AddSongs(new List<Int64> { s_FirstSong });

                Application.Library.Broadcast.PlaySong(s_FirstSong, s_QueueIDs[s_FirstSong]);
            }
            else if (Application.Library.Broadcast.PlayingSongID != 0)
            {
                QueueManager.UpdateQueue();
            }

            // Disable mobile compliance if needed.
            if (!SettingsManager.MobileCompliance())
                DisableMobileCompliance();
        }

        public static void DisableMobileCompliance()
        {
            Application.Library.Broadcast.DisableMobileCompliance();
        }

        private static void OnBroadcastCreationFailed(SharkEvent p_SharkEvent)
        {
            CreatingBroadcast = false;
        }

        public static bool AddGuest(String p_Username, Int64 p_UserID, VIPPermissions p_Permissions)
        {
            using (var s_Db = Database.GetConnection())
            {
                var s_Guest = s_Db.SingleById<SpecialGuest>(p_UserID);

                if (s_Guest != null)
                    return false;

                s_Guest = new SpecialGuest()
                {
                    Username = p_Username,
                    UserID = p_UserID,
                    Permissions = p_Permissions
                };

                s_Db.Insert(s_Guest);

                return true;
            }
        }

        public static bool RemoveGuest(Int64 p_UserID, out SpecialGuest p_Guest)
        {
            using (var s_Db = Database.GetConnection())
            {
                p_Guest = s_Db.SingleById<SpecialGuest>(p_UserID);

                if (p_Guest == null)
                    return false;

                s_Db.Delete(p_Guest);

                return true;
            }
        }

        public static bool RemoveGuest(Int64 p_UserID)
        {
            using (var s_Db = Database.GetConnection())
            {
                var s_Guest = s_Db.SingleById<SpecialGuest>(p_UserID);

                if (s_Guest == null)
                    return false;

                s_Db.Delete(s_Guest);

                return true;
            }
        }

        public static void UnguestAll()
        {
            foreach (var s_UserID in Application.Library.Broadcast.SpecialGuests)
            {
                Application.Library.Broadcast.RemoveSpecialGuest(s_UserID);
                return;
            }
        }

        public static SpecialGuest GetGuestForUserID(Int64 p_UserID)
        {
            using (var s_Db = Database.GetConnection())
                return s_Db.SingleById<SpecialGuest>(p_UserID);
        }

        public static bool HasActiveGuest(Int64 p_UserID)
        {
            return Application.Library.Broadcast.SpecialGuests.Contains(p_UserID);
        }

        public static void MakeGuest(SpecialGuest p_Guest)
        {
            Application.Library.Broadcast.AddSpecialGuest(p_Guest.UserID, p_Guest.Permissions);
        }

        public static void MakeGuest(Int64 p_UserID, VIPPermissions p_Permissions)
        {
            Application.Library.Broadcast.AddSpecialGuest(p_UserID, p_Permissions);
        }

        public static void Unguest(SpecialGuest p_Guest)
        {
            Application.Library.Broadcast.RemoveSpecialGuest(p_Guest.UserID);
        }

        public static void Unguest(Int64 p_UserID)
        {
            Application.Library.Broadcast.RemoveSpecialGuest(p_UserID);
        }

        public static void SetTitle(String p_Title)
        {
            Application.Library.Broadcast.UpdateBroadcastName(p_Title);
        }

        public static void SetDescription(String p_Description)
        {
            Application.Library.Broadcast.UpdateBroadcastDescription(p_Description);
        }

        public static bool CanUseCommands(SpecialGuest p_Guest)
        {
            if (p_Guest == null)
                return false;

            if (SettingsManager.CanCommandWithoutGuest())
                return true;

            return HasActiveGuest(p_Guest.UserID);
        }
    }
}
