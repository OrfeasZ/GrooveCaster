using System;
using System.Security.Cryptography;
using System.Text;
using GrooveCaster.Managers;
using GrooveCaster.Models;
using GS.Lib.Enums;
using Nancy;
using Nancy.ModelBinding;
using Nancy.Responses;
using Nancy.Security;
using ServiceStack.OrmLite;

namespace GrooveCaster.Modules
{
    public class DashboardModule : NancyModule
    {
        public DashboardModule()
        {
            this.RequiresAuthentication();

            Get["/"] = p_Parameters =>
            {
                if (String.IsNullOrWhiteSpace(Program.SecretKey))
                    return View["Error", new { ErrorText = "Failed to fetch SecretKey from GrooveShark.<br/>Please make sure GrooveCaster is up-to-date and that you're not banned from GrooveShark." }];
                
                using (var s_Db = Database.GetConnection())
                {
                    var s_GSUsername = s_Db.SingleById<CoreSetting>("gsun");
                    var s_GSPassword = s_Db.SingleById<CoreSetting>("gspw");

                    if (s_GSUsername == null || s_GSPassword == null)
                        return new RedirectResponse("/setup");
                }

                var s_Status = 0; // Broadcasting

                if (UserManager.Authenticating)
                {
                    s_Status = 1; // Authenticating
                }
                else if (!UserManager.Authenticating && UserManager.AuthenticationResult != AuthenticationResult.Success)
                {
                    s_Status = 2; // Authentication Failure
                }
                else if (!UserManager.Authenticating &&
                         UserManager.AuthenticationResult == AuthenticationResult.Success &&
                         BroadcastManager.CreatingBroadcast)
                {
                    s_Status = 3; // Creating Broadcast
                }
                else if (!UserManager.Authenticating &&
                         UserManager.AuthenticationResult == AuthenticationResult.Success &&
                         !BroadcastManager.CreatingBroadcast &&
                         Program.Library.Broadcast.ActiveBroadcastID == null &&
                         QueueManager.CollectionSongs.Count < 2)
                {
                    s_Status = 4; // Not enough songs
                }
                else if (!UserManager.Authenticating &&
                         UserManager.AuthenticationResult == AuthenticationResult.Success &&
                         !BroadcastManager.CreatingBroadcast &&
                         Program.Library.Broadcast.ActiveBroadcastID == null)
                {
                    s_Status = 5; // Broadcast creation failed.
                }

                return View["Index", new
                {
                    Status = s_Status,
                    ModuleErrors = ModuleManager.LoadExceptions
                }];
            };

            Get["/me/settings"] = p_Parameters =>
            {
                return View["Settings", new { Error = "" }];
            };

            Post["/me/settings"] = p_Parameters =>
            {
                var s_Request = this.Bind<UpdateAccountSettingsRequest>();

                if (s_Request.Password != s_Request.Repeat)
                    return View["Settings", new { Error = "The passwords you specified don't match." }];

                using (var s_Db = Database.GetConnection())
                {
                    var s_User = s_Db.Single<AdminUser>(p_User => p_User.Username == Context.CurrentUser.UserName);
                    
                    // Hash password
                    using (SHA256 s_Sha1 = new SHA256Managed())
                    {
                        var s_HashBytes = s_Sha1.ComputeHash(Encoding.UTF8.GetBytes(s_Request.Verification));
                        var s_HashedPassword = BitConverter.ToString(s_HashBytes).Replace("-", "").ToLowerInvariant();

                        if (s_HashedPassword != s_User.Password)
                            return View["Settings", new { Error = "Your current password doesn't match the one on records." }];

                        // Hash new password.
                        s_HashBytes = s_Sha1.ComputeHash(Encoding.UTF8.GetBytes(s_Request.Password));
                        s_HashedPassword = BitConverter.ToString(s_HashBytes).Replace("-", "").ToLowerInvariant();

                        s_User.Password = s_HashedPassword;
                        s_Db.Update(s_User);
                        
                        return View["Settings", new { Error = "" }];
                    }
                }
            };
        }
    }
}