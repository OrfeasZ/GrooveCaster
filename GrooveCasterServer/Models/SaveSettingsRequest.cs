using System;

namespace GrooveCasterServer.Models
{
    public class SaveSettingsRequest
    {
        public int History { get; set; }
        public int Threshold { get; set; }
        public String Title { get; set; }
        public String Description { get; set; }
    }
}
