using System;
using System.Collections.Generic;
using System.Linq;
using GrooveCasterServer.Models;
using GS.Lib.Enums;
using GS.Lib.Events;
using ServiceStack;

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
            Program.Library.RegisterEventHandler(ClientEvent.QueueUpdated, OnQueueUpdated);
        }

        private static void OnQueueUpdated(SharkEvent p_SharkEvent)
        {
            UpdateQueue();
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

            Console.WriteLine("Currently playing song: {0} ({1})", s_Event.SongName, s_Event.SongID);

            if (s_Event.SongID == 0)
            {
                // We ran out of songs, how did this happen?
                // Quickly! Add two to the queue!
                var s_Random = new Random();
                var s_FirstSongIndex = s_Random.Next(0, CollectionSongs.Count - 1);
                var s_SecondSongIndex = s_Random.Next(0, CollectionSongs.Count - 1);

                var s_FirstSong = CollectionSongs[s_FirstSongIndex];
                var s_SecondSong = CollectionSongs[s_SecondSongIndex];

                while (s_SecondSong == s_FirstSong)
                {
                    s_SecondSongIndex = s_Random.Next(0, CollectionSongs.Count - 1);
                    s_SecondSong = CollectionSongs[s_SecondSongIndex];
                }

                var s_Songs = Program.Library.Broadcast.AddSongs(new List<Int64> { s_FirstSong, s_SecondSong });
                Program.Library.Broadcast.PlaySong(s_FirstSong, s_Songs[s_FirstSong]);
                return;
            }

            PlayedSongs.Add(s_Event.SongID);

            // Clear songs from history (if needed).
            for (var i = 0; i < PlayedSongs.Count - SettingsManager.MaxHistorySongs(); ++i)
                PlayedSongs.RemoveAt(0);

            UpdateQueue();
        }

        private static void UpdateQueue()
        {
            var s_Index = Program.Library.Queue.GetPlayingSongIndex();

            Console.WriteLine("Updating Queue. Current Song: {0} - Total Songs: {1}", s_Index, Program.Library.Queue.CurrentQueue.Count);

            // We're running out of songs; add from collection.
            if (s_Index + 1 >= Program.Library.Queue.CurrentQueue.Count || s_Index == -1)
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

                Console.WriteLine("Adding song {0} to queue (from collection).", s_SongID);

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
            if (s_Event.CurrentVotes <= s_Threshold)
                SkipSong();
        }

        public static void SkipSong()
        {
            if (Program.Library.Broadcast.ActiveBroadcastID == null || Program.Library.Broadcast.PlayingSongID == 0 ||
                Program.Library.Broadcast.PlayingSongQueueID == 0)
                return;

            // Get the next song ID.
            var s_Index = Program.Library.Queue.GetPlayingSongIndex();

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
            var s_Index = Program.Library.Queue.GetPlayingSongIndex();

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

            var s_Index = Program.Library.Queue.GetPlayingSongIndex();

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
            if (Program.Library.Broadcast.ActiveBroadcastID == null || Program.Library.Broadcast.PlayingSongID == 0 ||
                   Program.Library.Broadcast.PlayingSongQueueID == 0)
                return;

            // Get the next song ID.
            var s_Index = Program.Library.Queue.GetPlayingSongIndex();

            if (s_Index + 1 >= Program.Library.Queue.CurrentQueue.Count)
                return;

            var s_QueueIDs = new List<Int64>();

            for (var i = s_Index + 1; i < Program.Library.Queue.CurrentQueue.Count; ++i)
            {
                if (Program.Library.Queue.CurrentQueue[i].SongName.ToLowerInvariant().Contains(p_Name.ToLowerInvariant()))
                    s_QueueIDs.Add(Program.Library.Queue.CurrentQueue[i].QueueID);
            }

            Program.Library.Broadcast.RemoveSongs(s_QueueIDs);
        }

        public static void FetchLast()
        {
            if (Program.Library.Broadcast.ActiveBroadcastID == null || Program.Library.Broadcast.PlayingSongID == 0 ||
                     Program.Library.Broadcast.PlayingSongQueueID == 0)
                return;

            var s_Index = Program.Library.Queue.GetPlayingSongIndex();

            if (s_Index + 1 >= Program.Library.Queue.CurrentQueue.Count - 1)
                return;

            var s_SongData = Program.Library.Queue.CurrentQueue[Program.Library.Queue.CurrentQueue.Count - 1];

            Program.Library.Broadcast.MoveSongs(new List<Int64> { s_SongData.QueueID }, Program.Library.Queue.GetPlayingSongIndex() + 1);
        }

        public static void FetchByName(String p_Name)
        {
            if (Program.Library.Broadcast.ActiveBroadcastID == null || Program.Library.Broadcast.PlayingSongID == 0 ||
                        Program.Library.Broadcast.PlayingSongQueueID == 0)
                return;

            var s_Index = Program.Library.Queue.GetPlayingSongIndex();

            if (s_Index + 1 >= Program.Library.Queue.CurrentQueue.Count - 1)
                return;

            for (var i = s_Index + 1; i < Program.Library.Queue.CurrentQueue.Count; ++i)
            {
                if (Program.Library.Queue.CurrentQueue[i].SongName.ToLowerInvariant()
                    .Contains(p_Name.ToLowerInvariant()))
                {
                    Program.Library.Broadcast.MoveSongs(new List<Int64> { Program.Library.Queue.CurrentQueue[i].QueueID }, Program.Library.Queue.GetPlayingSongIndex() + 1);
                    break;
                }
            }
        }

        public static void Shuffle()
        {
             if (Program.Library.Broadcast.ActiveBroadcastID == null || Program.Library.Broadcast.PlayingSongID == 0 ||
                     Program.Library.Broadcast.PlayingSongQueueID == 0)
                return;

            var s_Index = Program.Library.Queue.GetPlayingSongIndex();

            var s_SongCount = Program.Library.Queue.CurrentQueue.Count - (s_Index + 1);

            if (s_SongCount <= 1)
                return;

            var s_Songs = new SongShuffleData[s_SongCount];

            for (var i = 0; i < s_SongCount; ++i)
            {
                s_Songs[i] = new SongShuffleData()
                {
                    LastIndex = i,
                    QueueID = Program.Library.Queue.CurrentQueue[s_Index + i + 1].QueueID
                };
            }

            s_Songs = s_Songs.OrderBy(s_Song => Guid.NewGuid()).ToArray();

            for (var i = 0; i < s_SongCount; ++i)
            {
                var s_Song = s_Songs[i];
                Program.Library.Broadcast.MoveSongs(new List<Int64> { s_Song.QueueID }, Program.Library.Queue.GetPlayingSongIndex() + 1 + i);
            }

            Program.Library.Chat.SendChatMessage("Shuffled " + s_SongCount + " songs.");
        }
    }
}
