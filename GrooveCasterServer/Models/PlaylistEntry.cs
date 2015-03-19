using System;
using ServiceStack.DataAnnotations;

namespace GrooveCaster.Models
{
    public class PlaylistEntry
    {
        [PrimaryKey]
        public String ID { get { return PlaylistID + "/" + SongID; } }

        [Index]
        public Int64 PlaylistID { get; set; }

        public Int64 SongID { get; set; }

        public Int64 Index { get; set; }
    }
}
