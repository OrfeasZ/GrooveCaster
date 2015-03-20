using System;
using System.Linq;
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
                if (String.IsNullOrWhiteSpace(Application.SecretKey))
                    return View["Error", new { ErrorText = "Failed to fetch SecretKey from GrooveShark.<br/>Please make sure GrooveCaster is up-to-date and that you're not banned from GrooveShark." }];
                
                var s_Error = false;
                var s_Message = "Broadcast is created and broadcasting!";

                if (UserManager.Authenticating)
                {
                    s_Message = "Authenticating with the GS Backend...";
                }
                else if (!UserManager.Authenticating && UserManager.AuthenticationResult != AuthenticationResult.Success)
                {
                    s_Message = "Authentication failed. Please check your settings.";
                    s_Error = true;
                }
                else if (!UserManager.Authenticating &&
                         UserManager.AuthenticationResult == AuthenticationResult.Success &&
                         BroadcastManager.CreatingBroadcast)
                {
                    s_Message = "Creating and initializing broadcast parameters...";
                }
                else if (!UserManager.Authenticating &&
                         UserManager.AuthenticationResult == AuthenticationResult.Success &&
                         !BroadcastManager.CreatingBroadcast &&
                         Application.Library.Broadcast.ActiveBroadcastID == null &&
                         QueueManager.CollectionSongs.Count < 2)
                {
                    s_Message = "Broadcast didn't start. Not enough songs in collection. Use the \"Song Management\" interface to add songs.";
                    s_Error = true;
                }
                else if (!UserManager.Authenticating &&
                         UserManager.AuthenticationResult == AuthenticationResult.Success &&
                         !BroadcastManager.CreatingBroadcast &&
                         Application.Library.Broadcast.ActiveBroadcastID == null)
                {
                    s_Message = "Broadcast creation failed. Please check your settings.";
                    s_Error = true;
                }

                var s_Index = QueueManager.GetPlayingSongIndex();

                return View["Index", new
                {
                    Songs = QueueManager.GetUpcomingSongs(),
                    Playing = s_Index != -1,
                    Song = s_Index != -1 ? QueueManager.GetCurrentQueue()[s_Index] : null,
                    Message = s_Message,
                    Error = s_Error,
                    SuperUser = Context.CurrentUser.Claims.Contains("super"),
                    ModuleErrors = ModuleManager.LoadExceptions.Count > 0 && Context.CurrentUser.Claims.Contains("super")
                }];
            };

            Get["/me/settings"] = p_Parameters =>
            {
                return View["Settings", new { SuperUser = Context.CurrentUser.Claims.Contains("super"), HasError = false, Error = "" }];
            };

            Post["/me/settings"] = p_Parameters =>
            {
                var s_Request = this.Bind<UpdateAccountSettingsRequest>();

                if (s_Request.Password != s_Request.Repeat)
                    return View["Settings", new { HasError = true, Error = "The passwords you specified don't match." }];

                using (var s_Db = Database.GetConnection())
                {
                    var s_User = s_Db.Single<AdminUser>(p_User => p_User.Username == Context.CurrentUser.UserName);
                    
                    // Hash password
                    using (SHA256 s_Sha1 = new SHA256Managed())
                    {
                        var s_HashBytes = s_Sha1.ComputeHash(Encoding.UTF8.GetBytes(s_Request.Verification));
                        var s_HashedPassword = BitConverter.ToString(s_HashBytes).Replace("-", "").ToLowerInvariant();

                        if (s_HashedPassword != s_User.Password)
                            return View["Settings", new { HasError = true, Error = "Your current password doesn't match the one on records." }];

                        // Hash new password.
                        s_HashBytes = s_Sha1.ComputeHash(Encoding.UTF8.GetBytes(s_Request.Password));
                        s_HashedPassword = BitConverter.ToString(s_HashBytes).Replace("-", "").ToLowerInvariant();

                        s_User.Password = s_HashedPassword;
                        s_Db.Update(s_User);

                        return View["Settings", new { SuperUser = Context.CurrentUser.Claims.Contains("super"), HasError = false, Error = "" }];
                    }
                }
            };
        }
    }
}