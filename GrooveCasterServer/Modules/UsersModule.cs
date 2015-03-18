using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using GrooveCaster.Models;
using Nancy;
using Nancy.ModelBinding;
using Nancy.Responses;
using Nancy.Security;
using ServiceStack.OrmLite;

namespace GrooveCaster.Modules
{
    public class UsersModule : NancyModule
    {
        public UsersModule()
        {
            this.RequiresAuthentication();

            Get["/users"] = p_Parameters =>
            {
                if (!Context.CurrentUser.Claims.Contains("super"))
                    return new RedirectResponse("/");

                using (var s_Db = Database.GetConnection())
                    return View["Users", new { SuperUser = Context.CurrentUser.Claims.Contains("super"), Users = s_Db.Select<AdminUser>() }];
            };

            Get["/users/add"] = p_Parameters =>
            {
                if (!Context.CurrentUser.Claims.Contains("super"))
                    return new RedirectResponse("/");

                return View["AddUser", new { SuperUser = Context.CurrentUser.Claims.Contains("super"), Error = "" }];
            };

            Post["/users/add"] = p_Parameters =>
            {
                if (!Context.CurrentUser.Claims.Contains("super"))
                    return new RedirectResponse("/");

                var s_Request = this.Bind<LoginRequest>();

                if (s_Request.Username.Length < 3 || s_Request.Password.Length < 3)
                    return View["AddUser", new { SuperUser = Context.CurrentUser.Claims.Contains("super"), Error = "The information you provided is invalid." }];

                using (var s_Db = Database.GetConnection())
                {
                    var s_Username = s_Request.Username.Trim().ToLowerInvariant();
                    var s_User = s_Db.Single<AdminUser>(p_User => p_User.Username == s_Username);

                    if (s_User != null)
                        return View["AddUser", new { SuperUser = Context.CurrentUser.Claims.Contains("super"), Error = "A user with the specified username already exists." }];

                    using (SHA256 s_Sha1 = new SHA256Managed())
                    {
                        var s_HashBytes = s_Sha1.ComputeHash(Encoding.UTF8.GetBytes(s_Request.Password));
                        var s_HashedPassword = BitConverter.ToString(s_HashBytes).Replace("-", "").ToLowerInvariant();

                        s_User = new AdminUser()
                        {
                            Username = s_Username,
                            Password = s_HashedPassword,
                            UserID = Guid.NewGuid()
                        };

                        s_Db.Insert(s_User);
                    }
                }

                return new RedirectResponse("/users");
            };

            Get["/users/delete/{guid:guid}"] = p_Parameters =>
            {
                if (!Context.CurrentUser.Claims.Contains("super"))
                    return new RedirectResponse("/");

                Guid s_GUID = p_Parameters.guid;

                using (var s_Db = Database.GetConnection())
                {
                    var s_User = s_Db.SingleById<AdminUser>(s_GUID);

                    if (s_User == null || s_User.Username == "admin")
                        return new RedirectResponse("/users");

                    s_Db.Delete(s_User);

                    return new RedirectResponse("/users");
                }
            };
        }
    }
}
