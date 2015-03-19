using System;

namespace GrooveCaster.Models
{
    public class ImportSongsFromUserRequest
    {
        public Int64 User { get; set; }

        public bool Favorites { get; set; }

        public bool Only { get; set; }
    }
}
