using System;

namespace GrooveCaster.Models
{
    public class LoadPlaylistRequest
    {
        public Int64 Playlist { get; set; }

        public bool Shuffle { get; set; }
    }
}
