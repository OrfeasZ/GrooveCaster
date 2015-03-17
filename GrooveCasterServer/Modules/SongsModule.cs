using System;
using System.Collections.Generic;
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
    public class SongsModule : NancyModule
    {
        public SongsModule()
        {
            this.RequiresAuthentication();
            
            Get["/songs"] = p_Parameters =>
            {
                using (var s_Db = Database.GetConnection())
                    return View["Songs", new { Songs = s_Db.Select<SongEntry>() }];
            };

            Get["/songs/add"] = p_Parameters =>
            {
                return View["AddSong", new { Error = "" }];
            };

            Get["/songs/autocomplete/{query}.json"] = p_Parameters =>
            {
                String s_Query = p_Parameters.query;
                var s_Results = Program.Library.Search.GetAutocomplete(s_Query, "song");

                if (s_Results.ContainsKey("song"))
                    return s_Results["song"];

                return new List<ResultData>();
            };

            Post["/songs/add"] = p_Parameters =>
            {
                var s_Request = this.Bind<AddSongRequest>();

                if (s_Request.SongID <= 0 || s_Request.AlbumID <= 0 || s_Request.ArtistID <= 0 ||
                    String.IsNullOrWhiteSpace(s_Request.Song) || String.IsNullOrWhiteSpace(s_Request.Album) ||
                    String.IsNullOrWhiteSpace(s_Request.Artist))
                {
                    return View["AddSong", new { Error = "Please fill in all the required fields." }];
                }

                var s_PreviousSongCount = QueueManager.CollectionSongs.Count;

                using (var s_Db = Database.GetConnection())
                {
                    var s_Song = s_Db.SingleById<SongEntry>(s_Request.SongID);

                    if (s_Song != null)
                        return View["AddSong", new { Error = "The specified song already exists in your collection." }];

                    s_Song = new SongEntry()
                    {
                        AlbumName = s_Request.Album,
                        AlbumID = s_Request.AlbumID,
                        ArtistID = s_Request.ArtistID,
                        ArtistName = s_Request.Artist,
                        SongID = s_Request.SongID,
                        SongName = s_Request.Song
                    };

                    s_Db.Insert(s_Song);
                    QueueManager.CollectionSongs.Add(s_Song.SongID);
                }

                if (s_PreviousSongCount < 2 && QueueManager.CollectionSongs.Count >= 2)
                    BroadcastManager.CreateBroadcast();

                return new RedirectResponse("/songs");
            };

            Get["/songs/delete/{song:long}"] = p_Parameters =>
            {
                Int64 s_SongID = p_Parameters.song;

                // Don't allow the user to delete the last two songs.
                if (QueueManager.CollectionSongs.Count <= 2)
                    return new RedirectResponse("/songs");

                using (var s_Db = Database.GetConnection())
                {
                    var s_Song = s_Db.SingleById<SongEntry>(s_SongID);

                    if (s_Song == null)
                        return new RedirectResponse("/songs");

                    QueueManager.CollectionSongs.Remove(s_SongID);
                    s_Db.Delete(s_Song);
                }

                return new RedirectResponse("/songs");
            };

            Get["/songs/import"] = p_Parameters =>
            {
                return View["ImportSongs", new { User = Program.Library.User.Data.UserID }];
            };

            Get["/songs/import/autocomplete/{query}.json"] = p_Parameters =>
            {
                String s_Query = p_Parameters.query;

                var s_Results = Program.Library.Search.GetAutocomplete(s_Query, "user");

                if (s_Results.ContainsKey("user"))
                    return s_Results["user"];

                return new List<ResultData>();
            };

            Post["/songs/import"] = p_Parameters =>
            {
                var s_Request = this.Bind<ImportSongsFromUserRequest>();

                var s_PreviousSongCount = QueueManager.CollectionSongs.Count;

                var s_Songs = Program.Library.User.GetSongsInLibrary(s_Request.User);

                var s_SongEntries = new List<SongEntry>();

                foreach (var s_Song in s_Songs)
                {
                    try
                    {
                        var s_SongEntry = new SongEntry()
                        {
                            SongID = Int64.Parse(s_Song.SongID),
                            SongName = s_Song.Name,
                            ArtistID = Int64.Parse(s_Song.ArtistID),
                            ArtistName = s_Song.ArtistName,
                            AlbumID = Int64.Parse(s_Song.AlbumID),
                            AlbumName = s_Song.AlbumName
                        };

                        s_SongEntries.Add(s_SongEntry);
                    }
                    catch
                    {
                        continue;
                    }
                }

                using (var s_Db = Database.GetConnection())
                    s_Db.SaveAll(s_SongEntries);

                QueueManager.FetchCollectionSongs();

                if (s_PreviousSongCount < 2 && QueueManager.CollectionSongs.Count >= 2)
                    BroadcastManager.CreateBroadcast();

                return new RedirectResponse("/songs");
            };
        }
    }
}
