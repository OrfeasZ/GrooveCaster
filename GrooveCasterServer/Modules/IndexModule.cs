using System;
using GrooveCasterServer.Models;
using Nancy;
using Nancy.Responses;
using Nancy.Security;
using ServiceStack.OrmLite;

namespace GrooveCasterServer.Modules
{
    public class IndexModule : NancyModule
    {
        public IndexModule()
        {
            this.RequiresAuthentication();

            Get["/"] = p_Parameters =>
            {
                if (String.IsNullOrWhiteSpace(Program.SecretKey))
                    return View["Error", new { ErrorText = "Failed to fetch SecretKey from GrooveShark.<br/>Please make sure GrooveCaster is up-to-date and that you're not banned from GrooveShark." }];
                
                using (var s_Db = Program.DbConnectionString.OpenDbConnection())
                {
                    var s_GSUsername = s_Db.Single<CoreSetting>(p_Setting => p_Setting.Key == "gsun");
                    var s_GSPassword = s_Db.Single<CoreSetting>(p_Setting => p_Setting.Key == "gspw");

                    if (s_GSUsername == null || s_GSPassword == null)
                        return new RedirectResponse("/setup");
                }

                return View["Index"];
            };
        }
    }
}