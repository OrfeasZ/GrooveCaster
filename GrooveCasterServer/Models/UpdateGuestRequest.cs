namespace GrooveCaster.Models
{
    public class UpdateGuestRequest
    {
        public byte Permissions { get; set; }

        public bool Title { get; set; }

        public bool Description { get; set; }

        public bool Permanent { get; set; }

        public bool Temporary { get; set; }

        public bool Super { get; set; }
    }
}
