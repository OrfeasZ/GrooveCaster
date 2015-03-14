using System;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using GrooveCasterServer.Managers;
using GrooveCasterServer.Models;
using GS.Lib.Enums;
using Nancy;
using Nancy.ModelBinding;
using Nancy.Responses;
using Nancy.Security;
using ServiceStack.OrmLite;

namespace GrooveCasterServer.Modules
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
                
                using (var s_Db = Program.DbConnectionString.OpenDbConnection())
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
                         Program.Library.Broadcast.ActiveBroadcastID == null)
                {
                    s_Status = 4; // Broadcast Creation Failed
                }

                return View["Index", new
                {
                    Status = s_Status
                }];
            };

            Get["/status"] = p_Parameters =>
            {
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
                         Program.Library.Broadcast.ActiveBroadcastID == null)
                {
                    s_Status = 4; // Broadcast Creation Failed
                }

                return new {Status = s_Status};
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

                using (var s_Db = Program.DbConnectionString.OpenDbConnection())
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

            Get["/settings"] = p_Parameters =>
            {
                return View["CoreSettings", new
                {
                    History = SettingsManager.MaxHistorySongs(),
                    Threshold = SettingsManager.SongVoteThreshold(),
                    Title = BroadcastManager.GetBroadcastName(),
                    Description = BroadcastManager.GetBroadcastDescription()
                }];
            };

            Post["/settings"] = p_Parameters =>
            {
                var s_Request = this.Bind<SaveSettingsRequest>();

                SettingsManager.MaxHistorySongs(s_Request.History);
                SettingsManager.SongVoteThreshold(s_Request.Threshold);

                using (var s_Db = Program.DbConnectionString.OpenDbConnection())
                {
                    if (s_Request.Title.Trim().Length > 3)
                    {
                        s_Db.Update(new CoreSetting() {Key = "bcname", Value = s_Request.Title.Trim() });
                        Program.Library.Broadcast.UpdateBroadcastName(s_Request.Title.Trim());
                    }

                    if (s_Request.Description.Trim().Length > 3)
                    {
                        s_Db.Update(new CoreSetting() { Key = "bcdesc", Value = s_Request.Description.Trim() });
                        Program.Library.Broadcast.UpdateBroadcastDescription(s_Request.Description.Trim());
                    }
                }

                return View["CoreSettings", new
                {
                    History = SettingsManager.MaxHistorySongs(),
                    Threshold = SettingsManager.SongVoteThreshold(),
                    Title = BroadcastManager.GetBroadcastName(),
                    Description = BroadcastManager.GetBroadcastDescription()
                }];
            };

            Get["/guests"] = p_Parameters =>
            {
                using (var s_Db = Program.DbConnectionString.OpenDbConnection())
                {
                    var s_Guests = s_Db.Select<SpecialGuest>();

                    return View["Guests", new { Guests = s_Guests }];
                }
            };

            Post["/guests/update/{id:long}"] = p_Parameters =>
            {
                var s_Request = this.Bind<UpdateGuestRequest>();

                Int64 s_GuestID = p_Parameters.id;

                using (var s_Db = Program.DbConnectionString.OpenDbConnection())
                {
                    var s_SpecialGuest = s_Db.SingleById<SpecialGuest>(s_GuestID);

                    if (s_SpecialGuest == null)
                        return new RedirectResponse("/guests");

                    s_SpecialGuest.Permissions = (VIPPermissions) s_Request.Permissions;
                    s_SpecialGuest.CanAddPermanentGuests = s_Request.Permanent;
                    s_SpecialGuest.CanAddTemporaryGuests = s_Request.Temporary;
                    s_SpecialGuest.CanEditDescription = s_Request.Description;
                    s_SpecialGuest.CanEditTitle = s_Request.Title;
                    s_SpecialGuest.SuperGuest = s_Request.Super;

                    s_Db.Update(s_SpecialGuest);
                }

                return new RedirectResponse("/guests");
            };

            Get["/guests/delete/{id:long}"] = p_Parameters =>
            {
                Int64 s_GuestID = p_Parameters.id;

                using (var s_Db = Program.DbConnectionString.OpenDbConnection())
                {
                    var s_SpecialGuest = s_Db.SingleById<SpecialGuest>(s_GuestID);

                    if (s_SpecialGuest == null)
                        return new RedirectResponse("/guests");

                    s_Db.Delete(s_SpecialGuest);
                }

                return new RedirectResponse("/guests");
            };

            Get["/guests/add"] = p_Parameters =>
            {
                return View["AddGuest"];
            };

            Post["/guests/add"] = p_Parameters =>
            {
                var s_Reqest = this.Bind<AddGuestRequest>();

                using (var s_Db = Program.DbConnectionString.OpenDbConnection())
                {
                    var s_SpecialGuest = s_Db.SingleById<SpecialGuest>(s_Reqest.User);

                    if (s_SpecialGuest != null)
                        return new RedirectResponse("/guests");

                    s_SpecialGuest = new SpecialGuest()
                    {
                        UserID = s_Reqest.User,
                        Username = s_Reqest.Username,
                        CanAddPermanentGuests = s_Reqest.Permanent,
                        CanAddTemporaryGuests = s_Reqest.Temporary,
                        CanEditDescription = s_Reqest.Description,
                        CanEditTitle = s_Reqest.Title,
                        Permissions = (VIPPermissions) s_Reqest.Permissions,
                        SuperGuest = s_Reqest.Super
                    };

                    s_Db.Insert(s_SpecialGuest);
                }

                return new RedirectResponse("/guests");
            };

            Get["/guests/import"] = p_Parameters =>
            {
                var s_FollowingUsers = Program.Library.User.GetFollowingUsers();

                using (var s_Db = Program.DbConnectionString.OpenDbConnection())
                {
                    foreach (var s_User in s_FollowingUsers)
                    {
                        var s_SpecialGuest = s_Db.SingleById<SpecialGuest>(Int64.Parse(s_User.UserID));

                        if (s_SpecialGuest != null)
                            continue;

                        s_SpecialGuest = new SpecialGuest()
                        {
                            UserID = Int64.Parse(s_User.UserID),
                            Username = s_User.FName,
                            CanAddPermanentGuests = false,
                            CanAddTemporaryGuests = false,
                            CanEditDescription = false,
                            CanEditTitle = false,
                            Permissions = VIPPermissions.ChatModerate | VIPPermissions.Suggestions,
                            SuperGuest = false
                        };

                        s_Db.Insert(s_SpecialGuest);
                    }
                }

                return new RedirectResponse("/guests");
            };

            Get["/users"] = p_Parameters =>
            {
                using (var s_Db = Program.DbConnectionString.OpenDbConnection())
                    return View["Users", new { Users = s_Db.Select<AdminUser>() }];
            };

            Get["/users/add"] = p_Parameters =>
            {
                return View["AddUser", new { Error = "" }];
            };

            Post["/users/add"] = p_Parameters =>
            {
                var s_Request = this.Bind<LoginRequest>();

                if (s_Request.Username.Length < 3 || s_Request.Password.Length < 3)
                    return View["AddUser", new { Error = "The information you provided is invalid." }];

                using (var s_Db = Program.DbConnectionString.OpenDbConnection())
                {
                    var s_Username = s_Request.Username.Trim().ToLowerInvariant();
                    var s_User = s_Db.Single<AdminUser>(p_User => p_User.Username == s_Username);

                    if (s_User != null)
                        return View["AddUser", new { Error = "A user with the specified username already exists." }];
                    
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
                Guid s_GUID = p_Parameters.guid;

                using (var s_Db = Program.DbConnectionString.OpenDbConnection())
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