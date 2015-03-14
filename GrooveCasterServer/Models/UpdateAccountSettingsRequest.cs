using System;

namespace GrooveCasterServer.Models
{
    public class UpdateAccountSettingsRequest
    {
        public String Verification { get; set; }

        public String Password { get; set; }

        public String Repeat { get; set; }
    }
}
