using System;
using System.Collections.Generic;
using Nancy.Security;

namespace GrooveCasterServer.Nancy
{
    public class UserIdentity : IUserIdentity
    {
        public Guid UserID { get; set; }

        public string UserName { get; set; }

        public IEnumerable<string> Claims { get; set; }
    }
}
