using System;
using System.Security.Cryptography;
using System.Text;
using GrooveCasterServer.Models;
using Nancy;
using Nancy.Authentication.Forms;
using Nancy.ModelBinding;
using Nancy.Responses;
using ServiceStack.OrmLite;

namespace GrooveCasterServer.Modules
{
    public class AuthModule : NancyModule
    {
        public AuthModule()
        {
            Get["/login"] = p_Parameters =>
            {
                if (Context.CurrentUser != null)
                    return new RedirectResponse("/");

                return View["Login"];
            };

            Get["/logout"] = p_Parameters => this.LogoutAndRedirect("/login");

            Post["/login"] = p_Parameters =>
            {
                var s_Request = this.Bind<LoginRequest>();

                using (var s_Db = Program.DbConnectionString.OpenDbConnection())
                {
                    var s_User = s_Db.Single<AdminUser>(p_User => p_User.Username == s_Request.Username);
                    
                    if (s_User == null)
                        return View["Login", new { Error = "Invalid credentials specified." }];

                    // Hash password
                    using (SHA256 s_Sha1 = new SHA256Managed())
                    {
                        var s_HashBytes = s_Sha1.ComputeHash(Encoding.UTF8.GetBytes(s_Request.Password));
                        var s_HashedPassword = BitConverter.ToString(s_HashBytes).Replace("-", "").ToLowerInvariant();

                        if (s_HashedPassword != s_User.Password)
                            return View["Login", new { Error = "Invalid credentials specified." }];
                    }

                    return this.LoginAndRedirect(s_User.UserID);
                }
            };
        }
    }
}
