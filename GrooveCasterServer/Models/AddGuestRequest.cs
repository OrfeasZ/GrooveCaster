using System;

namespace GrooveCaster.Models
{
    public class AddGuestRequest
    {
        public Int64 User { get; set; }

        public String Username { get; set; }

        public byte Permissions { get; set; }

        public bool Title { get; set; }

        public bool Description { get; set; }

        public bool Permanent { get; set; }

        public bool Temporary { get; set; }

        public bool Super { get; set; }
    }
}
