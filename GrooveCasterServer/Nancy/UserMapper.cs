using System;
using System.Collections.Generic;
using GrooveCaster.Models;
using Nancy;
using Nancy.Authentication.Forms;
using Nancy.Security;
using ServiceStack.OrmLite;

namespace GrooveCaster.Nancy
{
    public class UserMapper : IUserMapper
    {
        public IUserIdentity GetUserFromIdentifier(Guid p_Identifier, NancyContext p_Context)
        {
            using (var s_Db = Database.GetConnection())
            {
                var s_User = s_Db.SingleById<AdminUser>(p_Identifier);

                if (s_User == null)
                    return null;

                var s_Identity = new UserIdentity()
                {
                    UserID = p_Identifier,
                    UserName = s_User.Username,
                    Claims = new List<String>() { "admin" }
                };

                if (s_User.Superuser)
                    ((List<String>) s_Identity.Claims).Add("super");

                return s_Identity;
            }
        }
    }
}
