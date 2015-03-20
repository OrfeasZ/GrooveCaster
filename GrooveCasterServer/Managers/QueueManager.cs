using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using GrooveCaster.Models;
using GS.Lib.Enums;
using GS.Lib.Events;
using GS.Lib.Models;
using ServiceStack.OrmLite;

namespace GrooveCaster.Managers
{
    public static class QueueManager
    {
        public static List<Int64> CollectionSongs { get; set; } 

        public static List<Int64> PlayedSongs { get; set; }

        static QueueManager()
        {
            PlayedSongs = new List<long>();
            CollectionSongs = new List<long>();
        }

        internal static void Init()
        {
            PlayedSongs.Clear();
            CollectionSongs.Clear();
            
            Application.Library.RegisterEventHandler(ClientEvent.SongPlaying, OnSongPlaying);
            Application.Library.RegisterEventHandler(ClientEvent.SongVote, OnSongVote);
            Application.Library.RegisterEventHandler(ClientEvent.QueueUpdated, OnQueueUpdated);
        }

        private static void OnQueueUpdated(SharkEvent p_SharkEvent)
        {
            UpdateQueue();
        }

        public static void FetchCollectionSongs()
        {
            using (var s_Db = Database.GetConnection())
            {
                var s_Songs = s_Db.Select<SongEntry>();

                CollectionSongs = s_Songs.Select(p_Song => p_Song.SongID).ToList();
            }
        }

        public static void ClearHistory()
        {
            PlayedSongs.Clear();
        }

        public static void QueueRandomSongs(int p_Count)
        {
            Application.Library.Broadcast.AddSongs(GetRandomSongIDs(p_Count));
        }

        public static List<Int64> GetRandomSongIDs(int p_Count)
        {
            var s_Songs = new List<Int64>();

            var s_Random = new Random();
            for (var i = 0; i < p_Count; ++i)
            {
                var s_RandomSongIndex = s_Random.Next(0, CollectionSongs.Count);

                var s_SongID = CollectionSongs[s_RandomSongIndex];

                if (CollectionSongs.Count <= PlayedSongs.Count)
                    PlayedSongs.Clear();

                // Make sure the song we're adding is within our history limits.
                while (PlayedSongs.Contains(s_SongID) || s_Songs.Contains(s_SongID))
                {
                    s_RandomSongIndex = s_Random.Next(0, CollectionSongs.Count);
                    s_SongID = CollectionSongs[s_RandomSongIndex];
                }

                s_Songs.Add(s_SongID);
            }

            return s_Songs;
        }

        private static void OnSongPlaying(SharkEvent p_SharkEvent)
        {
            var s_Event = (SongPlayingEvent)p_SharkEvent;

            Trace.WriteLine(String.Format("Currently playing song: {0} ({1})", s_Event.SongName, s_Event.SongID));

            if (s_Event.SongID == 0)
            {
                // We stopped playing, how did this happen?
                // Quickly! Add two to the queue!

                // Allows for custom queuing logic by Modules.
                if (!ModuleManager.OnFetchingNextSong())
                    return;

                if (PlaylistManager.PlaylistActive && PlaylistManager.HasNextSong())
                {
                    var s_SongID = PlaylistManager.DequeueNextSong();
                    var s_QueueIDs = Application.Library.Broadcast.AddSongs(new List<Int64> { s_SongID });
                    Application.Library.Broadcast.PlaySong(s_SongID, s_QueueIDs[s_SongID]);
                    return;
                }

                var s_Random = new Random();
                var s_FirstSongIndex = s_Random.Next(0, CollectionSongs.Count);
                var s_SecondSongIndex = s_Random.Next(0, CollectionSongs.Count);

                var s_FirstSong = CollectionSongs[s_FirstSongIndex];
                var s_SecondSong = CollectionSongs[s_SecondSongIndex];

                while (s_SecondSong == s_FirstSong)
                {
                    s_SecondSongIndex = s_Random.Next(0, CollectionSongs.Count);
                    s_SecondSong = CollectionSongs[s_SecondSongIndex];
                }

                var s_Songs = Application.Library.Broadcast.AddSongs(new List<Int64> { s_FirstSong, s_SecondSong });
                Application.Library.Broadcast.PlaySong(s_FirstSong, s_Songs[s_FirstSong]);
                return;
            }

            PlayedSongs.Add(s_Event.SongID);

            // Clear songs from history (if needed).
            for (var i = 0; i < PlayedSongs.Count - SettingsManager.MaxHistorySongs(); ++i)
                PlayedSongs.RemoveAt(0);

            UpdateQueue();
        }

        internal static void UpdateQueue()
        {
            var s_Index = Application.Library.Queue.GetPlayingSongIndex();

            Trace.WriteLine(String.Format("Updating Queue. Current Song: {0} - Total Songs: {1}", s_Index, Application.Library.Queue.CurrentQueue.Count));

            // We're running out of songs; add new ones.
            if (s_Index + 1 >= Application.Library.Queue.CurrentQueue.Count)
            {
                // Allows for custom queuing logic by Modules.
                if (!ModuleManager.OnFetchingNextSong())
                    return;

                if (PlaylistManager.PlaylistActive && PlaylistManager.HasNextSong())
                {
                    var s_PlaylistSongID = PlaylistManager.DequeueNextSong();
                    Application.Library.Broadcast.AddSongs(new List<Int64> { s_PlaylistSongID });
                    return;
                }

                var s_Random = new Random();
                var s_RandomSongIndex = s_Random.Next(0, CollectionSongs.Count);

                var s_SongID = CollectionSongs[s_RandomSongIndex];

                if (CollectionSongs.Count <= PlayedSongs.Count)
                    PlayedSongs.Clear();

                // Make sure the song we're adding is within our history limits.
                while (PlayedSongs.Contains(s_SongID))
                {
                    s_RandomSongIndex = s_Random.Next(0, CollectionSongs.Count);
                    s_SongID = CollectionSongs[s_RandomSongIndex];
                }

                Trace.WriteLine(String.Format("Adding song {0} to queue (from collection).", s_SongID));

                Application.Library.Broadcast.AddSongs(new List<Int64> { s_SongID });
            }
        }

        private static void OnSongVote(SharkEvent p_SharkEvent)
        {
            var s_Event = (SongVoteEvent) p_SharkEvent;

            var s_Threshold = SettingsManager.SongVoteThreshold();

            if (s_Threshold == 0)
                return;

            // Automatically skip a song if it reaches a number of negative votes.
            if (s_Event.CurrentVotes <= s_Threshold)
                SkipSong();
        }

        public static void SkipSong()
        {
            if (Application.Library.Broadcast.ActiveBroadcastID == null || Application.Library.Broadcast.PlayingSongID == 0 ||
                Application.Library.Broadcast.PlayingSongQueueID == 0)
                return;

            // Get the next song ID.
            var s_Index = Application.Library.Queue.GetPlayingSongIndex();

            if (s_Index + 1 >= Application.Library.Queue.CurrentQueue.Count)
                return;

            var s_NextSong = Application.Library.Queue.CurrentQueue[s_Index + 1];

            Application.Library.Broadcast.PlaySong(s_NextSong.SongID, s_NextSong.QueueID);
        }

        public static void RemoveNext(int p_Count = 1)
        {
            if (Application.Library.Broadcast.ActiveBroadcastID == null || Application.Library.Broadcast.PlayingSongID == 0 ||
                Application.Library.Broadcast.PlayingSongQueueID == 0)
                return;

            // Get the next song ID.
            var s_Index = Application.Library.Queue.GetPlayingSongIndex();

            if (s_Index + 1 >= Application.Library.Queue.CurrentQueue.Count)
                return;

            var s_QueueIDs = new List<Int64>();

            for (var i = 0; i < p_Count; ++i)
            {
                if (s_Index + i + 1 >= Application.Library.Queue.CurrentQueue.Count)
                    break;

                s_QueueIDs.Add(Application.Library.Queue.CurrentQueue[s_Index + i + 1].QueueID);
            }

            Application.Library.Broadcast.RemoveSongs(s_QueueIDs);
        }

        public static void RemoveLast(int p_Count = 1)
        {
            if (Application.Library.Broadcast.ActiveBroadcastID == null || Application.Library.Broadcast.PlayingSongID == 0 ||
                  Application.Library.Broadcast.PlayingSongQueueID == 0)
                return;

            var s_Index = Application.Library.Queue.GetPlayingSongIndex();

            if (s_Index + 1 >= Application.Library.Queue.CurrentQueue.Count)
                return;

            var s_QueueIDs = new List<Int64>();

            for (var i = p_Count - 1; i >= 0; --i)
            {
                if (Application.Library.Queue.CurrentQueue.Count - 1 - i == s_Index)
                    break;

                s_QueueIDs.Add(Application.Library.Queue.CurrentQueue[Application.Library.Queue.CurrentQueue.Count - 1 - i].QueueID);
            }

            Application.Library.Broadcast.RemoveSongs(s_QueueIDs);
        }

        public static void RemoveByName(String p_Name)
        {
            if (Application.Library.Broadcast.ActiveBroadcastID == null || Application.Library.Broadcast.PlayingSongID == 0 ||
                   Application.Library.Broadcast.PlayingSongQueueID == 0)
                return;

            // Get the next song ID.
            var s_Index = Application.Library.Queue.GetPlayingSongIndex();

            if (s_Index + 1 >= Application.Library.Queue.CurrentQueue.Count)
                return;

            var s_QueueIDs = new List<Int64>();

            for (var i = s_Index + 1; i < Application.Library.Queue.CurrentQueue.Count; ++i)
            {
                if (Application.Library.Queue.CurrentQueue[i].SongName.ToLowerInvariant().Contains(p_Name.ToLowerInvariant()))
                    s_QueueIDs.Add(Application.Library.Queue.CurrentQueue[i].QueueID);
            }

            Application.Library.Broadcast.RemoveSongs(s_QueueIDs);
        }

        public static void FetchLast()
        {
            if (Application.Library.Broadcast.ActiveBroadcastID == null || Application.Library.Broadcast.PlayingSongID == 0 ||
                     Application.Library.Broadcast.PlayingSongQueueID == 0)
                return;

            var s_Index = Application.Library.Queue.GetPlayingSongIndex();

            if (s_Index + 1 >= Application.Library.Queue.CurrentQueue.Count - 1)
                return;

            var s_SongData = Application.Library.Queue.CurrentQueue[Application.Library.Queue.CurrentQueue.Count - 1];

            Application.Library.Broadcast.MoveSongs(new List<Int64> { s_SongData.QueueID }, Application.Library.Queue.GetPlayingSongIndex() + 1);
        }

        public static void FetchByName(String p_Name)
        {
            if (Application.Library.Broadcast.ActiveBroadcastID == null || Application.Library.Broadcast.PlayingSongID == 0 ||
                        Application.Library.Broadcast.PlayingSongQueueID == 0)
                return;

            var s_Index = Application.Library.Queue.GetPlayingSongIndex();

            if (s_Index + 1 >= Application.Library.Queue.CurrentQueue.Count - 1)
                return;

            for (var i = s_Index + 1; i < Application.Library.Queue.CurrentQueue.Count; ++i)
            {
                if (Application.Library.Queue.CurrentQueue[i].SongName.ToLowerInvariant()
                    .Contains(p_Name.ToLowerInvariant()))
                {
                    Application.Library.Broadcast.MoveSongs(new List<Int64> { Application.Library.Queue.CurrentQueue[i].QueueID }, Application.Library.Queue.GetPlayingSongIndex() + 1);
                    break;
                }
            }
        }

        public static void Shuffle()
        {
             if (Application.Library.Broadcast.ActiveBroadcastID == null || Application.Library.Broadcast.PlayingSongID == 0 ||
                     Application.Library.Broadcast.PlayingSongQueueID == 0)
                return;

            var s_Index = Application.Library.Queue.GetPlayingSongIndex();

            var s_SongCount = Application.Library.Queue.CurrentQueue.Count - (s_Index + 1);

            if (s_SongCount <= 1)
                return;

            var s_Songs = new SongShuffleData[s_SongCount];

            for (var i = 0; i < s_SongCount; ++i)
            {
                s_Songs[i] = new SongShuffleData()
                {
                    LastIndex = i,
                    QueueID = Application.Library.Queue.CurrentQueue[s_Index + i + 1].QueueID
                };
            }

            s_Songs = s_Songs.OrderBy(s_Song => Guid.NewGuid()).ToArray();

            var s_QueueIDs = new List<Int64>();

            for (var i = 0; i < s_SongCount; ++i)
                s_QueueIDs.Add(s_Songs[i].QueueID);

            Application.Library.Broadcast.MoveSongs(s_QueueIDs, Application.Library.Queue.GetPlayingSongIndex() + 1);

            Application.Library.Chat.SendChatMessage("Shuffled " + s_SongCount + " songs.");
        }

        public static bool AddPlayingSongToCollection()
        {
            using (var s_Db = Database.GetConnection())
            {
                var s_Song = s_Db.SingleById<SongEntry>(Application.Library.Broadcast.PlayingSongID);

                if (s_Song != null)
                    return false;

                s_Song = new SongEntry()
                {
                    AlbumID = Application.Library.Broadcast.PlayingAlbumID,
                    AlbumName = Application.Library.Broadcast.PlayingSongAlbum,
                    ArtistID = Application.Library.Broadcast.PlayingArtistID,
                    ArtistName = Application.Library.Broadcast.PlayingSongArtist,
                    SongID = Application.Library.Broadcast.PlayingSongID,
                    SongName = Application.Library.Broadcast.PlayingSongName
                };

                s_Db.Insert(s_Song);
                CollectionSongs.Add(s_Song.SongID);

                return true;
            }
        }

        public static bool RemovePlayingSongFromCollection()
        {
            using (var s_Db = Database.GetConnection())
            {
                var s_Song = s_Db.SingleById<SongEntry>(Application.Library.Broadcast.PlayingSongID);

                if (s_Song == null)
                    return false;

                CollectionSongs.Remove(s_Song.SongID);
                s_Db.Delete(s_Song);

                return true;
            }
        }

        public static int GetPlayingSongIndex()
        {
            return Application.Library.Queue.GetPlayingSongIndex();
        }

        public static List<QueueSongData> GetCurrentQueue()
        {
            return Application.Library.Queue.CurrentQueue;
        }

        public static void SeekCurrentSong(float p_Seconds)
        {
            Application.Library.Broadcast.SeekCurrentSong(p_Seconds);
        }

        public static void EmptyQueue()
        {
            var s_SongsToRemove = new List<Int64>();

            var s_Index = GetPlayingSongIndex();
            var s_SongCount = GetCurrentQueue().Count - s_Index - 1;

            for (var i = s_Index + 1; i < s_Index + 1 + s_SongCount; ++i)
                s_SongsToRemove.Add(GetCurrentQueue()[i].QueueID);

            Application.Library.Broadcast.RemoveSongs(s_SongsToRemove);
        }

        public static void QueueSong(Int64 p_SongID)
        {
            Application.Library.Broadcast.AddSongs(new List<Int64>() { p_SongID });
        }

        public static void QueueSongs(List<Int64> p_SongIDs)
        {
            Application.Library.Broadcast.AddSongs(p_SongIDs);
        }

        public static void MoveSong(Int64 p_QueueID, int p_Index)
        {
            Application.Library.Broadcast.MoveSongs(new List<Int64> { p_QueueID }, p_Index);
        }

        public static void MoveSongs(List<Int64> p_QueueIDs, int p_Index)
        {
            Application.Library.Broadcast.MoveSongs(p_QueueIDs, p_Index);
        }

        public static void RemoveSong(Int64 p_QueueID)
        {
            Application.Library.Broadcast.RemoveSongs(new List<Int64> { p_QueueID });
        }

        public static void RemoveSongs(List<Int64> p_QueueIDs)
        {
            Application.Library.Broadcast.RemoveSongs(p_QueueIDs);
        }

        public static void PlaySong(Int64 p_QueueID)
        {
            var s_SongIndex = Application.Library.Queue.GetInternalIndexForSong(p_QueueID);

            if (s_SongIndex == -1)
                return;

            var s_Song = Application.Library.Queue.CurrentQueue[s_SongIndex];

            Application.Library.Broadcast.PlaySong(s_Song.SongID, s_Song.QueueID);
        }

        public static int GetSongIndex(Int64 p_QueueID)
        {
            return Application.Library.Queue.GetInternalIndexForSong(p_QueueID);
        }

        public static int GetSongIDIndex(Int64 p_SongID)
        {
            return Application.Library.Queue.GetInternalIndexForSongID(p_SongID);
        }

        public static List<QueueSongData> GetUpcomingSongs()
        {
            var s_UpcomingSongs = new List<QueueSongData>();

            var s_Index = GetPlayingSongIndex();
            var s_SongCount = GetCurrentQueue().Count - s_Index - 1;

            for (var i = s_Index + 1; i < s_Index + 1 + s_SongCount; ++i)
                s_UpcomingSongs.Add(GetCurrentQueue()[i]);

            return s_UpcomingSongs;
        }
    }
}
