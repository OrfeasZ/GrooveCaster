using System;
using System.Collections.Generic;

namespace GrooveCaster.Models
{
    public class SongSuggestion
    {
        public Int64 SongID { get; set; }

        public String SongName { get; set; }

        public Int64 ArtistID { get; set; }

        public String ArtistName { get; set; }

        public Int64 AlbumID { get; set; }

        public String AlbumName { get; set; }

        public SimpleUser Suggester { get; set; }

        public List<SimpleUser> OtherSuggesters { get; set; } 
    }
}
