using System;
using System.Collections.Generic;
using System.Linq;
using GrooveCaster.Managers;
using GrooveCaster.Models;
using GS.Lib;
using Nancy;
using Nancy.ModelBinding;
using Nancy.Responses;
using Nancy.Security;
using ServiceStack.OrmLite;

namespace GrooveCaster.Modules
{
    public class SettingsModule : NancyModule
    {
        public SettingsModule()
        {
            this.RequiresAuthentication();

            Get["/settings"] = p_Parameters =>
            {
                if (!Context.CurrentUser.Claims.Contains("super"))
                    return new RedirectResponse("/");

                return View["CoreSettings", new
                {
                    SuperUser = Context.CurrentUser.Claims.Contains("super"),
                    History = SettingsManager.MaxHistorySongs(),
                    Threshold = SettingsManager.SongVoteThreshold(),
                    Title = BroadcastManager.GetBroadcastName(),
                    Description = BroadcastManager.GetBroadcastDescription(),
                    CommandPrefix = SettingsManager.CommandPrefix().ToString(),
                    WithoutGuest = SettingsManager.CanCommandWithoutGuest()
                }];
            };

            Post["/settings"] = p_Parameters =>
            {
                if (!Context.CurrentUser.Claims.Contains("super"))
                    return new RedirectResponse("/");

                var s_Request = this.Bind<SaveSettingsRequest>();

                SettingsManager.MaxHistorySongs(s_Request.History);
                SettingsManager.SongVoteThreshold(s_Request.Threshold);
                SettingsManager.CommandPrefix(s_Request.Prefix[0]);
                SettingsManager.CanCommandWithoutGuest(s_Request.Guest);

                using (var s_Db = Database.GetConnection())
                {
                    if (s_Request.Title.Trim().Length > 3)
                    {
                        s_Db.Update(new CoreSetting() { Key = "bcname", Value = s_Request.Title.Trim() });
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
                    SuperUser = Context.CurrentUser.Claims.Contains("super"),
                    History = SettingsManager.MaxHistorySongs(),
                    Threshold = SettingsManager.SongVoteThreshold(),
                    Title = BroadcastManager.GetBroadcastName(),
                    Description = BroadcastManager.GetBroadcastDescription(),
                    CommandPrefix = SettingsManager.CommandPrefix().ToString(),
                    WithoutGuest = SettingsManager.CanCommandWithoutGuest()
                }];
            };

            Get["/settings/reset"] = p_Parameters =>
            {
                if (!Context.CurrentUser.Claims.Contains("super"))
                    return new RedirectResponse("/");

                using (var s_Db = Database.GetConnection())
                {
                    // Delete all settings.
                    s_Db.DeleteByIds<CoreSetting>(new List<String> { "gsun", "gspw", "gssess", "bcmobile", "bcname", "bcdesc", 
                        "bctag", "cmdprefix", "cmdguest", "votethreshold", "history" });

                    // Stop the broadcast.
                    Program.Library.Broadcast.DestroyBroadcast();

                    // Re-initialize SharpShark.
                    Program.Library.Chat.Disconnect();
                    Program.Library = new SharpShark(Program.SecretKey);
                    Program.BootstrapLibrary();

                    return new RedirectResponse("/setup");
                }
            };
        }
    }
}
