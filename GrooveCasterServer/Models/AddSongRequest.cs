using System;

namespace GrooveCasterServer.Models
{
    public class AddSongRequest
    {
        public Int64 SongID { get; set; }

        public String Song { get; set; }

        public Int64 ArtistID { get; set; }

        public String Artist { get; set; }

        public Int64 AlbumID { get; set; }

        public String Album { get; set; }
    }
}
