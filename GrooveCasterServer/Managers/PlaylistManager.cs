using System;
using System.Collections.Generic;
using System.Linq;
using GrooveCaster.Models;
using GS.Lib.Models;
using Microsoft.Scripting.Utils;
using Nancy.Conventions;
using Nancy.Extensions;
using ServiceStack;
using ServiceStack.OrmLite;

namespace GrooveCaster.Managers
{
    public static class PlaylistManager
    {
        public static bool PlaylistActive { get; private set; }

        public static Playlist ActivePlaylist { get; private set; }

        private static Queue<PlaylistEntry> m_Entries;

        private static Queue<Playlist> m_QueuedPlaylists;

        private static Dictionary<Int64, bool> m_QueuedPlaylistsShuffle; 

        static PlaylistManager()
        {
        }

        internal static void Init()
        {
            PlaylistActive = false;
            ActivePlaylist = null;
            m_Entries = new Queue<PlaylistEntry>();
            m_QueuedPlaylists = new Queue<Playlist>();
            m_QueuedPlaylistsShuffle = new Dictionary<long, bool>();
        }

        public static bool HasNextSong()
        {
            return m_Entries.Count > 0;
        }

        public static Int64 PeekNextSong()
        {
            return m_Entries.Peek().SongID;
        }

        public static Int64 DequeueNextSong()
        {
            var s_NextSongID = m_Entries.Dequeue().SongID;

            if (m_Entries.Count != 0) 
                return s_NextSongID;

            // We ran out of songs; disable this playlist or play the next one.
            if (m_QueuedPlaylists.Count > 0)
            {
                var s_NextPlaylist = m_QueuedPlaylists.Dequeue();
                var s_Shuffle = m_QueuedPlaylistsShuffle[s_NextPlaylist.ID];
                m_QueuedPlaylistsShuffle.Remove(s_NextPlaylist.ID);
                LoadPlaylist(s_NextPlaylist, s_Shuffle);
            }
            else
            {
                PlaylistActive = false;
                ActivePlaylist = null;
            }

            return s_NextSongID;
        }

        public static bool LoadPlaylist(Playlist p_Playlist, bool p_Shuffle)
        {
            PlaylistActive = false;
            m_Entries.Clear();

            ActivePlaylist = p_Playlist;

            if (ActivePlaylist == null)
                return false;

            using (var s_Db = Database.GetConnection())
            {
                var s_Songs = s_Db.Select<PlaylistEntry>(p_Entry => p_Entry.PlaylistID == ActivePlaylist.ID);

                // Sort or shuffle the songs.
                if (!p_Shuffle)
                    s_Songs = s_Songs.OrderBy(p_Entry => p_Entry.Index).ToList();
                else
                    s_Songs = s_Songs.OrderBy(s_Song => Guid.NewGuid()).ToList();

                // Queue up all the songs.
                foreach (var s_Entry in s_Songs)
                    m_Entries.Enqueue(s_Entry);

                if (m_Entries.Count > 0)
                    PlaylistActive = true;

                return true;
            }
        }

        public static bool LoadPlaylist(Int64 p_ID, bool p_Shuffle)
        {
            if (m_QueuedPlaylists.Any(p_Playlist => p_Playlist.ID == p_ID))
                return false;

            using (var s_Db = Database.GetConnection())
            {
                return LoadPlaylist(s_Db.SingleById<Playlist>(p_ID), p_Shuffle);
            }
        }

        public static void DisablePlaylist()
        {
            PlaylistActive = false;

            if (m_QueuedPlaylists.Count > 0)
            {
                var s_NextPlaylist = m_QueuedPlaylists.Dequeue();
                var s_Shuffle = m_QueuedPlaylistsShuffle[s_NextPlaylist.ID];
                m_QueuedPlaylistsShuffle.Remove(s_NextPlaylist.ID);
                LoadPlaylist(s_NextPlaylist, s_Shuffle);
            }
            else
            {
                ActivePlaylist = null;
                m_Entries.Clear();
            }
        }

        public static void ClearPlaylist()
        {
            DisablePlaylist();
            ActivePlaylist = null;
            m_Entries.Clear();
            m_QueuedPlaylists.Clear();
            m_QueuedPlaylistsShuffle.Clear();
        }

        public static void EnablePlaylist()
        {
            PlaylistActive = true;
        }

        public static void ImportPlaylistsForUser(Int64 p_UserID)
        {
            var s_Playlists = Application.Library.User.GetPlaylists(p_UserID);

            foreach (var s_Playlist in s_Playlists)
                ImportPlaylist(s_Playlist.PlaylistID);
        }

        public static void ImportPlaylist(Int64 p_ExternalID)
        {
            var s_PlaylistData = Application.Library.User.GetPlaylistData(p_ExternalID);

            if (s_PlaylistData == null)
                return;
            
            using (var s_Db = Database.GetConnection())
            {
                var s_Playlist = s_Db.Single<Playlist>(p_Playlist => p_Playlist.GrooveSharkID == s_PlaylistData.PlaylistID);

                if (s_Playlist == null)
                {
                    s_Playlist = new Playlist()
                    {
                        Description = s_PlaylistData.About,
                        Name = s_PlaylistData.Name,
                        GrooveSharkID = s_PlaylistData.PlaylistID
                    };
                }

                // Insert/update the playlist.
                s_Db.Save(s_Playlist);

                var s_Songs = new List<PlaylistEntry>();
                var s_NewSongs = new List<SongEntry>();
            
                foreach (var s_Song in s_PlaylistData.Songs)
                {
                    s_Songs.Add(new PlaylistEntry() { PlaylistID = s_Playlist.ID, SongID = s_Song.SongID, Index = 0 });

                    var s_LocalSong = s_Db.SingleById<SongEntry>(s_Song.SongID);

                    if (s_LocalSong != null)
                        continue;

                    s_LocalSong = new SongEntry()
                    {
                        SongID = s_Song.SongID,
                        SongName = s_Song.Name,
                        ArtistID = s_Song.ArtistID,
                        ArtistName = s_Song.ArtistName,
                        AlbumID = s_Song.AlbumID,
                        AlbumName = s_Song.AlbumName,
                    };

                    s_NewSongs.Add(s_LocalSong);
                }

                s_Songs = s_Songs.DistinctBy(p_Entry => p_Entry.SongID).ToList();
                s_NewSongs = s_NewSongs.DistinctBy(p_Song => p_Song.SongID).ToList();

                // Calculate indexes.
                for (var i = 0; i < s_Songs.Count; ++i)
                    s_Songs[i].Index = i;

                // Add songs that don't exist to the collection.
                s_Db.SaveAll(s_NewSongs);

                // Delete old song entries.
                s_Db.Delete<PlaylistEntry>(p_Entry => p_Entry.PlaylistID == s_Playlist.ID);

                // Import the new ones.
                s_Db.SaveAll(s_Songs);
            }
        }

        public static void DeleteAllPlaylists()
        {
            ClearPlaylist();

            using (var s_Db = Database.GetConnection())
            {
                s_Db.DeleteAll<PlaylistEntry>();
                s_Db.DeleteAll<Playlist>();
            }
        }

        public static void DeletePlaylist(Int64 p_PlaylistID)
        {
            using (var s_Db = Database.GetConnection())
            {
                // Delete song entries.
                s_Db.Delete<PlaylistEntry>(p_Entry => p_Entry.PlaylistID == p_PlaylistID);

                // Delete the playlist.
                s_Db.DeleteById<Playlist>(p_PlaylistID);
            }
        }

        public static void RemoveSongFromPlaylist(Int64 p_PlaylistID, Int64 p_SongID)
        {
            using (var s_Db = Database.GetConnection())
            {
                var s_Playlist = s_Db.SingleById<Playlist>(p_PlaylistID);

                if (s_Playlist == null)
                    return;

                s_Db.Delete<PlaylistEntry>(p_Entry => p_Entry.PlaylistID == p_PlaylistID && p_Entry.SongID == p_SongID);

                // TODO: Remove the song from the currently playing playlist (if it's the same)?
            }
        }

        public static bool AddSongToPlaylist(Int64 p_PlaylistID, Int64 p_SongID)
        {
            using (var s_Db = Database.GetConnection())
            {
                var s_Playlist = s_Db.SingleById<Playlist>(p_PlaylistID);

                if (s_Playlist == null)
                    return false;

                var s_Entry = s_Db.Single<PlaylistEntry>(p_Entry => p_Entry.PlaylistID == p_PlaylistID && 
                    p_Entry.SongID == p_SongID);

                if (s_Entry != null)
                    return true;

                var s_Song = s_Db.SingleById<SongEntry>(p_SongID);

                if (s_Song == null)
                    return false;

                var s_LastSong = s_Db.SelectFmt<PlaylistEntry>("PlaylistID = {0} ORDER BY Index DESC LIMIT 1",
                    p_PlaylistID);

                s_Entry = new PlaylistEntry
                {
                    PlaylistID = p_PlaylistID,
                    SongID = p_SongID,
                    Index = s_LastSong.Count == 0 ? 0 : s_LastSong[0].Index + 1
                };

                s_Db.Insert(s_Entry);

                if (ActivePlaylist == null || ActivePlaylist.ID != p_PlaylistID)
                    return true;

                m_Entries.Enqueue(s_Entry);

                return true;
            }
        }

        public static List<Playlist> GetPlaylists()
        {
            using (var s_Db = Database.GetConnection())
            {
                var s_Playlists = s_Db.Select<Playlist>();

                foreach (var s_Playlist in m_QueuedPlaylists)
                {
                    var s_PlaylistIndex = s_Playlists.FindIndex(p_Playlist => p_Playlist.ID == s_Playlist.ID);

                    if (s_PlaylistIndex == -1)
                        continue;

                    s_Playlists.RemoveAt(s_PlaylistIndex);
                }

                if (ActivePlaylist != null)
                {
                    var s_Index = s_Playlists.FindIndex(p_Playlist => p_Playlist.ID == ActivePlaylist.ID);

                    if (s_Index != -1)
                        s_Playlists.RemoveAt(s_Index);
                }

                return s_Playlists;
            }
        }

        public static Playlist GetPlaylist(Int64 p_ID)
        {
            using (var s_Db = Database.GetConnection())
                return s_Db.SingleById<Playlist>(p_ID);
        }

        public static List<Playlist> GetQueuedPlaylists()
        {
            return m_QueuedPlaylists.ToList();
        }

        public static bool QueuePlaylist(Int64 p_PlaylistID, bool p_Shuffle)
        {
            var s_Playlist = GetPlaylist(p_PlaylistID);

            if (s_Playlist == null)
                return false;

            if (ActivePlaylist == null)
                return false;

            if (ActivePlaylist.ID == s_Playlist.ID)
                return false;

            if (m_QueuedPlaylists.Any(p_Playlist => p_Playlist.ID == s_Playlist.ID))
                return false;

            m_QueuedPlaylists.Enqueue(s_Playlist);
            m_QueuedPlaylistsShuffle.Add(s_Playlist.ID, p_Shuffle);

            return true;
        }

        public static void DequeuePlaylist(Int64 p_Playlist)
        {
            var s_UpdatedQueue = new Queue<Playlist>();

            foreach (var s_Playlist in m_QueuedPlaylists)
            {
                if (s_Playlist.ID == p_Playlist)
                    continue;

                s_UpdatedQueue.Enqueue(s_Playlist);
            }

            m_QueuedPlaylistsShuffle.Remove(p_Playlist);
            m_QueuedPlaylists = s_UpdatedQueue;
        }

        public static List<PlaylistEntry> GetPlaylistSongs(Int64 p_PlaylistID)
        {
            using (var s_Db = Database.GetConnection())
                return s_Db.Select<PlaylistEntry>(p_Entry => p_Entry.PlaylistID == p_PlaylistID);
        }
    }
}
