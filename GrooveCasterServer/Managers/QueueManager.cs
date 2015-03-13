using System;
using System.Collections.Generic;
using GS.Lib.Enums;
using GS.Lib.Events;

namespace GrooveCasterServer.Managers
{
    public static class QueueManager
    {
        public static List<Int64> CollectionSongs { get; set; } 

        public static List<Int64> PlayedSongs { get; set; } 

        static QueueManager()
        {
            PlayedSongs = new List<long>();
        }

        public static void Init()
        {
            Program.Library.RegisterEventHandler(ClientEvent.SongPlaying, OnSongPlaying);
            Program.Library.RegisterEventHandler(ClientEvent.SongVote, OnSongVote);
        }

        public static void FetchCollectionSongs()
        {
            CollectionSongs = Program.Library.User.GetCollectionSongs();
        }

        public static void ClearHistory()
        {
            PlayedSongs.Clear();
        }

        private static void OnSongPlaying(SharkEvent p_SharkEvent)
        {
            var s_Event = (SongPlayingEvent)p_SharkEvent;

            PlayedSongs.Add(s_Event.SongID);

            // Clear songs from history (if needed).
            for (var i = 0; i < PlayedSongs.Count - SettingsManager.MaxHistorySongs(); ++i)
                PlayedSongs.RemoveAt(0);

            UpdateQueue();
        }

        private static void UpdateQueue()
        {
            // We're running out of songs; add from collection.
            if (Program.Library.Queue.GetInternalIndexForSong(Program.Library.Broadcast.PlayingSongQueueID) == Program.Library.Queue.CurrentQueue.Count - 1)
            {
                var s_Random = new Random();
                var s_RandomSongIndex = s_Random.Next(0, CollectionSongs.Count - 1);

                var s_SongID = CollectionSongs[s_RandomSongIndex];

                if (CollectionSongs.Count <= PlayedSongs.Count)
                    PlayedSongs.Clear();

                // Make sure the song we're adding is within our history limits.
                while (PlayedSongs.Contains(s_SongID))
                {
                    s_RandomSongIndex = s_Random.Next(0, CollectionSongs.Count - 1);
                    s_SongID = CollectionSongs[s_RandomSongIndex];
                }

                Program.Library.Broadcast.AddSongs(new List<Int64> { s_SongID });
            }
        }

        private static void OnSongVote(SharkEvent p_SharkEvent)
        {
            var s_Event = (SongVoteEvent) p_SharkEvent;

            var s_Threshold = SettingsManager.SongVoteThreshold();

            if (s_Threshold == 0)
                return;

            // Automatically skip a song if it reaches a number of negative votes.
            if (s_Event.CurrentVote <= s_Threshold)
                SkipSong();
        }

        public static void SkipSong()
        {
            if (Program.Library.Broadcast.ActiveBroadcastID == null || Program.Library.Broadcast.PlayingSongID == 0 ||
                Program.Library.Broadcast.PlayingSongQueueID == 0)
                return;

            // Get the next song ID.
            var s_Index = Program.Library.Queue.GetInternalIndexForSong(Program.Library.Broadcast.PlayingSongQueueID);

            if (s_Index + 1 >= Program.Library.Queue.CurrentQueue.Count)
                return;

            var s_NextSong = Program.Library.Queue.CurrentQueue[s_Index + 1];

            Program.Library.Broadcast.PlaySong(s_NextSong.SongID, s_NextSong.QueueID);
        }

        public static void RemoveNext(int p_Count = 1)
        {
            if (Program.Library.Broadcast.ActiveBroadcastID == null || Program.Library.Broadcast.PlayingSongID == 0 ||
                Program.Library.Broadcast.PlayingSongQueueID == 0)
                return;

            // Get the next song ID.
            var s_Index = Program.Library.Queue.GetInternalIndexForSong(Program.Library.Broadcast.PlayingSongQueueID);

            if (s_Index + 1 >= Program.Library.Queue.CurrentQueue.Count)
                return;

            var s_QueueIDs = new List<Int64>();

            for (var i = 0; i < p_Count; ++i)
            {
                if (s_Index + i + 1 >= Program.Library.Queue.CurrentQueue.Count)
                    break;

                s_QueueIDs.Add(Program.Library.Queue.CurrentQueue[s_Index + i + 1].QueueID);
            }

            Program.Library.Broadcast.RemoveSongs(s_QueueIDs);
        }

        public static void RemoveLast(int p_Count = 1)
        {
            if (Program.Library.Broadcast.ActiveBroadcastID == null || Program.Library.Broadcast.PlayingSongID == 0 ||
                  Program.Library.Broadcast.PlayingSongQueueID == 0)
                return;

            var s_Index = Program.Library.Queue.GetInternalIndexForSong(Program.Library.Broadcast.PlayingSongQueueID);

            if (s_Index + 1 >= Program.Library.Queue.CurrentQueue.Count)
                return;

            var s_QueueIDs = new List<Int64>();

            for (var i = p_Count - 1; i >= 0; --i)
            {
                if (Program.Library.Queue.CurrentQueue.Count - 1 - i == s_Index)
                    break;

                s_QueueIDs.Add(Program.Library.Queue.CurrentQueue[Program.Library.Queue.CurrentQueue.Count - 1 - i].QueueID);
            }

            Program.Library.Broadcast.RemoveSongs(s_QueueIDs);
        }

        public static void RemoveByName(String p_Name)
        {
            Program.Library.Chat.SendChatMessage("This feature has not been implemented yet.");
        }

        public static void FetchLast()
        {
            if (Program.Library.Broadcast.ActiveBroadcastID == null || Program.Library.Broadcast.PlayingSongID == 0 ||
                     Program.Library.Broadcast.PlayingSongQueueID == 0)
                return;

            var s_Index = Program.Library.Queue.GetInternalIndexForSong(Program.Library.Broadcast.PlayingSongQueueID);

            if (s_Index + 1 >= Program.Library.Queue.CurrentQueue.Count - 1)
                return;

            var s_SongData = Program.Library.Queue.CurrentQueue[Program.Library.Queue.CurrentQueue.Count - 1];

            Program.Library.Broadcast.MoveSongs(new List<Int64> { s_SongData.QueueID }, Program.Library.Queue.CurrentQueue[s_Index].Index + 1);
        }

        public static void FetchByName(String p_Name)
        {
            Program.Library.Chat.SendChatMessage("This feature has not been implemented yet.");
        }

        public static void Shuffle()
        {
            Program.Library.Chat.SendChatMessage("This feature has not been implemented yet.");
        }
    }
}
