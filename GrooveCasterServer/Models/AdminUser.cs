using System;
using ServiceStack.DataAnnotations;

namespace GrooveCasterServer.Models
{
    public class AdminUser
    {
        [PrimaryKey]
        public Guid UserID { get; set; }

        [Index(Unique = true)]
        public String Username { get; set; }

        public String Password { get; set; }
    }
}
