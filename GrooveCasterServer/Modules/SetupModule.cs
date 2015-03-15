using System;
using System.Linq;
using GrooveCasterServer.Managers;
using GrooveCasterServer.Models;
using Nancy;
using Nancy.ModelBinding;
using Nancy.Responses;
using Nancy.Security;
using ServiceStack.OrmLite;

namespace GrooveCasterServer.Modules
{
    public class SetupModule : NancyModule
    {
        public SetupModule()
        {
            this.RequiresAuthentication();

            Get["/setup"] = p_Parameters =>
            {
                if (!Context.CurrentUser.Claims.Contains("super"))
                    return new RedirectResponse("/");

                if (String.IsNullOrWhiteSpace(Program.SecretKey))
                    return View["Error", new { ErrorText = "Failed to fetch SecretKey from GrooveShark. Please make sure GrooveCaster is up-to-date and that you're not banned from GrooveShark." }];
                
                using (var s_Db = Program.DbConnectionString.OpenDbConnection())
                {
                    var s_GSUsername = s_Db.Single<CoreSetting>(p_Setting => p_Setting.Key == "gsun");
                    var s_GSPassword = s_Db.Single<CoreSetting>(p_Setting => p_Setting.Key == "gspw");

                    if (s_GSUsername != null && s_GSPassword != null)
                        return new RedirectResponse("/");

                    return View["Setup"];
                }
            };

            Post["/setup/check-credentials"] = p_Parameters =>
            {
                if (!Context.CurrentUser.Claims.Contains("super"))
                    return new RedirectResponse("/");

                var s_Request = this.Bind<LoginRequest>();

                var s_Result = Program.Library.User.Authenticate(s_Request.Username, s_Request.Password);

                return new { Result = s_Result };
            };

            Get["/setup/last-broadcast"] = p_Parameters =>
            {
                if (!Context.CurrentUser.Claims.Contains("super"))
                    return new RedirectResponse("/");

                var s_LastBroadcast = Program.Library.Broadcast.GetLastBroadcast();
                return s_LastBroadcast;
            };

            Get["/setup/category-tags"] = p_Parameters =>
            {
                if (!Context.CurrentUser.Claims.Contains("super"))
                    return new RedirectResponse("/");

                var s_Categories = Program.Library.Broadcast.GetCategoryTags();
                return s_Categories;
            };

            Post["/setup"] = p_Parameters =>
            {
                if (!Context.CurrentUser.Claims.Contains("super"))
                    return new RedirectResponse("/");
                
                var s_Request = this.Bind<SetupRequest>();

                if (String.IsNullOrWhiteSpace(s_Request.Username) || String.IsNullOrWhiteSpace(s_Request.Password) ||
                    String.IsNullOrWhiteSpace(s_Request.Title) || String.IsNullOrWhiteSpace(s_Request.Description) ||
                    String.IsNullOrWhiteSpace(s_Request.Tag) || String.IsNullOrWhiteSpace(Program.Library.User.SessionID))
                    return new RedirectResponse("/setup");

                using (var s_Db = Program.DbConnectionString.OpenDbConnection())
                {
                    var s_GSUsername = s_Db.Single<CoreSetting>(p_Setting => p_Setting.Key == "gsun");
                    var s_GSPassword = s_Db.Single<CoreSetting>(p_Setting => p_Setting.Key == "gspw");

                    if (s_GSUsername != null && s_GSPassword != null)
                        return new RedirectResponse("/");

                    s_Db.Insert(new CoreSetting() { Key = "gsun", Value = s_Request.Username });
                    s_Db.Insert(new CoreSetting() { Key = "gspw", Value = s_Request.Password });
                    s_Db.Insert(new CoreSetting() { Key = "gssess", Value = Program.Library.User.SessionID });
                    s_Db.Insert(new CoreSetting() { Key = "bcname", Value = s_Request.Title });
                    s_Db.Insert(new CoreSetting() { Key = "bcdesc", Value = s_Request.Description });
                    s_Db.Insert(new CoreSetting() { Key = "bctag", Value = s_Request.Tag });
                    s_Db.Insert(new CoreSetting() { Key = "bcmobile", Value = s_Request.Mobile.ToString() });
                }

                UserManager.Authenticate();

                return new RedirectResponse("/");
            };
        }
    }
}
