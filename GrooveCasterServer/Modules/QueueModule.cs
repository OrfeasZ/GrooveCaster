using System;
using System.Collections.Generic;
using System.Linq;
using GrooveCaster.Managers;
using GrooveCaster.Models;
using GS.Lib.Models;
using Nancy;
using Nancy.ModelBinding;
using Nancy.Responses;
using Nancy.Security;
using ServiceStack.OrmLite;

namespace GrooveCaster.Modules
{
    public class QueueModule : NancyModule
    {
        public QueueModule()
        {
            this.RequiresAuthentication();

            Get["/queue"] = p_Parameters =>
            {
                var s_Index = QueueManager.GetPlayingSongIndex();

                var s_Playlists = PlaylistManager.GetPlaylists();

                return View["Queue", new
                {
                    SuperUser = Context.CurrentUser.Claims.Contains("super"),
                    Songs = QueueManager.GetUpcomingSongs(),
                    PlaylistActive = PlaylistManager.PlaylistActive,
                    Playlist = PlaylistManager.ActivePlaylist,
                    Playing = s_Index != -1,
                    Song = s_Index != -1 ? QueueManager.GetCurrentQueue()[s_Index] : null,
                    Playlists = s_Playlists,
                    HasPlaylists = s_Playlists.Count > 0
                }];
            };

            Get["/queue/autocomplete/{query}.json"] = p_Parameters =>
            {
                String s_Query = p_Parameters.query;

                using (var s_Db = Database.GetConnection())
                    return s_Db.Select<SongEntry>(p_Entry => p_Entry.SongName.Contains(s_Query));
            };

            Get["/queue/song/skip"] = p_Parameters =>
            {
                QueueManager.SkipSong();
                return new RedirectResponse("/queue");
            };

            Post["/queue/song/add"] = p_Parameters =>
            {
                var s_Request = this.Bind<QueueSongRequest>();
                QueueManager.QueueSong(s_Request.Song);
                return new RedirectResponse("/queue");
            };

            Get["/queue/song/move-down/{id:long}"] = p_Parameters =>
            {
                Int64 s_QueueID = p_Parameters.id;

                var s_Index = QueueManager.GetPlayingSongIndex();
                var s_SongIndex = QueueManager.GetSongIndex(s_QueueID);

                if ( s_SongIndex <= s_Index + 1)
                    return new RedirectResponse("/queue");

                QueueManager.MoveSong(s_QueueID, s_SongIndex + 1);

                return new RedirectResponse("/queue");
            };

            Get["/queue/song/move-up/{id:long}"] = p_Parameters =>
            {
                Int64 s_QueueID = p_Parameters.id;

                var s_Index = QueueManager.GetPlayingSongIndex();
                var s_SongIndex = QueueManager.GetSongIndex(s_QueueID);

                if (s_SongIndex <= s_Index + 1 || s_SongIndex >= QueueManager.GetCurrentQueue().Count - 1)
                    return new RedirectResponse("/queue");

                QueueManager.MoveSong(s_QueueID, s_SongIndex - 1);

                return new RedirectResponse("/queue");
            };

            Get["/queue/song/remove/{id:long}"] = p_Parameters =>
            {
                Int64 s_QueueID = p_Parameters.id;

                QueueManager.RemoveSong(s_QueueID);

                return new RedirectResponse("/queue");
            };

            Get["/queue/song/play/{id:long}"] = p_Parameters =>
            {
                Int64 s_QueueID = p_Parameters.id;

                QueueManager.PlaySong(s_QueueID);

                return new RedirectResponse("/queue");
            };

            Get["/queue/empty"] = p_Parameters =>
            {
                QueueManager.EmptyQueue();
                return new RedirectResponse("/queue");
            };

            Get["/queue/playlist/disable"] = p_Parameters =>
            {
                PlaylistManager.DisablePlaylist();
                return new RedirectResponse("/queue");
            };

            Post["/queue/playlist/load"] = p_Parameters =>
            {
                var s_Request = this.Bind<LoadPlaylistRequest>();

                PlaylistManager.LoadPlaylist(s_Request.Playlist, s_Request.Shuffle);

                return new RedirectResponse("/queue");
            };
        }
    }
}
