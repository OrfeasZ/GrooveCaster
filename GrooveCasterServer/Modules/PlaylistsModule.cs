using System;
using System.Linq;
using System.Text;
using GrooveCaster.Managers;
using Nancy;
using Nancy.Responses;
using Nancy.Security;
using Newtonsoft.Json;

namespace GrooveCaster.Modules
{
    public class PlaylistsModule : NancyModule
    {
        public PlaylistsModule()
        {
            this.RequiresAuthentication();

            Get["/playlists"] = p_Parameters =>
            {
                return View["Playlists", new
                {
                    SuperUser = Context.CurrentUser.Claims.Contains("super"),
                    Playlists = PlaylistManager.GetPlaylists()
                }];
            };

            Get["/playlists/import"] = p_Parameters =>
            {
                return View["ImportPlaylists", new
                {
                    SuperUser = Context.CurrentUser.Claims.Contains("super")
                }];
            };

            Get["/playlists/fetch/{user:long}"] = p_Parameters =>
            {
                Int64 s_UserID = p_Parameters.user;

                var s_Songs = Program.Library.User.GetPlaylists(s_UserID);

                var s_Serialized = JsonConvert.SerializeObject(s_Songs);
                var s_Encoded = Encoding.UTF8.GetBytes(s_Serialized);

                return new Response()
                {
                    ContentType = "application/json",
                    Contents = p_Writer => p_Writer.Write(s_Encoded, 0, s_Encoded.Length)
                };
            };

            Get["/playlists/import/playlist/{id:long}"] = p_Parameters =>
            {
                Int64 s_PlaylistID = p_Parameters.id;

                PlaylistManager.ImportPlaylist(s_PlaylistID);

                return "";
            };

            Get["/playlists/import/user/{id:long}"] = p_Parameters =>
            {
                Int64 s_UserID = p_Parameters.id;

                PlaylistManager.ImportPlaylistsForUser(s_UserID);

                return "";
            };

            Get["/playlists/delete/{id:long}"] = p_Parameters =>
            {
                Int64 s_PlaylistID = p_Parameters.id;

                PlaylistManager.DeletePlaylist(s_PlaylistID);

                return new RedirectResponse("/playlists");
            };

            Get["/playlists/edit/{id:long}"] = p_Parameters =>
            {
                Int64 s_PlaylistID = p_Parameters.id;

                // TODO: Playlist editing

                return new RedirectResponse("/playlists");
            };
        }
    }
}
