﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GrooveCaster.Managers;
using GrooveCaster.Models;
using GS.Lib.Models;
using Nancy;
using Nancy.Extensions;
using Nancy.ModelBinding;
using Nancy.Responses;
using Nancy.Security;
using Newtonsoft.Json;
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
                return View["Songs", new { SuperUser = Context.CurrentUser.Claims.Contains("super") }];
            };

            Get["/songs/all.json"] = p_Parameters =>
            {
                using (var s_Db = Database.GetConnection())
                {
                    var s_Serialized = JsonConvert.SerializeObject(s_Db.Select<SongEntry>());
                    var s_Encoded = Encoding.UTF8.GetBytes(s_Serialized);

                    return new Response()
                    {
                        ContentType = "application/json",
                        Contents = p_Writer => p_Writer.Write(s_Encoded, 0, s_Encoded.Length)
                    };
                }
            };

            Get["/songs/add"] = p_Parameters =>
            {
                return View["AddSong", new { SuperUser = Context.CurrentUser.Claims.Contains("super"), Error = "" }];
            };

            Get["/songs/autocomplete/{query}.json"] = p_Parameters =>
            {
                String s_Query = p_Parameters.query;
                var s_Results = Application.Library.Search.GetAutocomplete(s_Query, "song");

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
                    return View["AddSong", new { SuperUser = Context.CurrentUser.Claims.Contains("super"), Error = "Please fill in all the required fields." }];
                }

                var s_PreviousSongCount = QueueManager.CollectionSongs.Count;

                using (var s_Db = Database.GetConnection())
                {
                    var s_Song = s_Db.SingleById<SongEntry>(s_Request.SongID);

                    if (s_Song != null)
                        return View["AddSong", new { SuperUser = Context.CurrentUser.Claims.Contains("super"), Error = "The specified song already exists in your collection." }];

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
                return View["ImportSongs", new { SuperUser = Context.CurrentUser.Claims.Contains("super"), User = Application.Library.User.Data.UserID }];
            };

            Get["/songs/import/autocomplete/{query}.json"] = p_Parameters =>
            {
                String s_Query = p_Parameters.query;

                var s_Results = Application.Library.Search.GetAutocomplete(s_Query, "user");

                if (s_Results.ContainsKey("user"))
                    return s_Results["user"];

                return new List<ResultData>();
            };

            Post["/songs/import"] = p_Parameters =>
            {
                var s_Request = this.Bind<ImportSongsFromUserRequest>();

                var s_PreviousSongCount = QueueManager.CollectionSongs.Count;

                var s_SongEntries = new List<SongEntry>();

                // Fetch collection songs.
                if (!s_Request.Only)
                {
                    var s_Songs = Application.Library.User.GetSongsInLibrary(s_Request.User);
                    
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
                }

                // Fetch favorite songs.
                if (s_Request.Favorites || s_Request.Only)
                {
                    var s_Favorites = Application.Library.User.GetFavorites("Songs", s_Request.User);

                    foreach (var s_Song in s_Favorites)
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
                }

                // Remove duplicates.
                s_SongEntries = s_SongEntries.DistinctBy(p_Entry => p_Entry.SongID).ToList();

                // Store
                using (var s_Db = Database.GetConnection())
                    s_Db.SaveAll(s_SongEntries);

                // Update loaded collection
                QueueManager.FetchCollectionSongs();

                // Create the broadcast (if needed).
                if (s_PreviousSongCount < 2 && QueueManager.CollectionSongs.Count >= 2)
                    BroadcastManager.CreateBroadcast();

                return new RedirectResponse("/songs");
            };
        }
    }
}
