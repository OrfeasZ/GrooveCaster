using System;
using System.Collections.Generic;
using GrooveCasterServer.Models;
using Nancy;
using Nancy.Authentication.Forms;
using Nancy.Security;
using ServiceStack.OrmLite;

namespace GrooveCasterServer.Nancy
{
    public class UserMapper : IUserMapper
    {
        public IUserIdentity GetUserFromIdentifier(Guid p_Identifier, NancyContext p_Context)
        {
            using (var s_Db = Program.DbConnectionString.OpenDbConnection())
            {
                var s_User = s_Db.SingleById<AdminUser>(p_Identifier);

                if (s_User == null)
                    return null;

                return new UserIdentity()
                {
                    UserID = p_Identifier,
                    UserName = s_User.Username,
                    Claims = new List<String>() { "admin" }
                };
            }
        }
    }
}
