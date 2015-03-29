using System;

namespace GrooveCaster.Models
{
    public class SongVoteUnit
    {
        public Int64 UserID { get; set; }
        public Int64 SongID { get; set; }
        public Int64 Vote { get; set; }
    }
}
