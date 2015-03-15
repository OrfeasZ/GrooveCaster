using System;
using GrooveCasterServer.Models;
using GS.Lib.Enums;
using Nancy;
using Nancy.ModelBinding;
using Nancy.Responses;
using Nancy.Security;
using ServiceStack.OrmLite;

namespace GrooveCasterServer.Modules
{
    public class GuestsModule : NancyModule
    {
        public GuestsModule()
        {
            this.RequiresAuthentication();

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

                    s_SpecialGuest.Permissions = (VIPPermissions)s_Request.Permissions;
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
                        Permissions = (VIPPermissions)s_Reqest.Permissions,
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
        }
    }
}
