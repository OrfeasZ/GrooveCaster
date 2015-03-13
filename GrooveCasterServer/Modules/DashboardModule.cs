using System;
using GrooveCasterServer.Managers;
using GrooveCasterServer.Models;
using GS.Lib.Enums;
using Nancy;
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
        }
    }
}