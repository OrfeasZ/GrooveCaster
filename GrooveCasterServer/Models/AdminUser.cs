using System;
using ServiceStack.DataAnnotations;

namespace GrooveCaster.Models
{
    public class AdminUser
    {
        [PrimaryKey]
        public Guid UserID { get; set; }

        [Index(Unique = true)]
        public String Username { get; set; }

        public String Password { get; set; }

        public bool Superuser { get; set; }
    }
}
