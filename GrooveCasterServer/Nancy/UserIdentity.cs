using System;
using System.Collections.Generic;
using Nancy.Security;

namespace GrooveCaster.Nancy
{
    public class UserIdentity : IUserIdentity
    {
        public Guid UserID { get; set; }

        public string UserName { get; set; }

        public IEnumerable<string> Claims { get; set; }
    }
}
