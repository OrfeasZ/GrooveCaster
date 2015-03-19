using System;
using ServiceStack.DataAnnotations;

namespace GrooveCaster.Models
{
    public class Playlist
    {
        [PrimaryKey, AutoIncrement]
        public Int64 ID { get; set; }

        public String Name { get; set; }

        public String Description { get; set; }

        [Index(Unique = true)]
        public Int64? GrooveSharkID { get; set; }
    }
}
