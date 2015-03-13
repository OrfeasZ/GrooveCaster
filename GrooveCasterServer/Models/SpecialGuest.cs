using System;
using GS.Lib.Enums;
using ServiceStack.DataAnnotations;

namespace GrooveCasterServer.Models
{
    public class SpecialGuest
    {
        [PrimaryKey]
        public Int64 UserID { get; set; }

        public String Username { get; set; }

        public VIPPermissions Permissions { get; set; }
    }
}
