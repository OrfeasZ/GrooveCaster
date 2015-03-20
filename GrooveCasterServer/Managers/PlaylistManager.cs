using System;
using System.Collections.Generic;
using System.Linq;
using GrooveCaster.Models;
using GS.Lib.Models;
using Nancy.Extensions;
using ServiceStack.OrmLite;

namespace GrooveCaster.Managers
{
    public static class PlaylistManager
    {
        public static bool PlaylistActive { get; private set; }

        public static Playlist ActivePlaylist { get; private set; }

        private static Queue<PlaylistEntry> m_Entries;

        static PlaylistManager()
        {
        }

        internal static void Init()
        {
            PlaylistActive = false;
            ActivePlaylist = null;
            m_Entries = new Queue<PlaylistEntry>();
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

            // We ran out of songs; disable this playlist.
            if (m_Entries.Count == 0)
            {
                PlaylistActive = false;
                ActivePlaylist = null;
            }

            return s_NextSongID;
        }

        public static bool LoadPlaylist(Int64 p_ID, bool p_Shuffle)
        {
            PlaylistActive = false;
            m_Entries.Clear();

            using (var s_Db = Database.GetConnection())
            {
                ActivePlaylist = s_Db.SingleById<Playlist>(p_ID);

                if (ActivePlaylist == null)
                    return false;

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

        public static void DisablePlaylist()
        {
            PlaylistActive = false;
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
                return s_Db.Select<Playlist>();
        }

        public static List<PlaylistEntry> GetPlaylistSongs(Int64 p_PlaylistID)
        {
            return null;
        }
    }
}
