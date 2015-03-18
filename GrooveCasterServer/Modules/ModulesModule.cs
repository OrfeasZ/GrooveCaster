using System;
using System.Linq;
using GrooveCaster.Managers;
using GrooveCaster.Models;
using Nancy;
using Nancy.ModelBinding;
using Nancy.Responses;
using Nancy.Security;
using ServiceStack.OrmLite;

namespace GrooveCaster.Modules
{
    public class ModulesModule : NancyModule
    {
        public ModulesModule()
        {
            this.RequiresAuthentication();

            Get["/modules"] = p_Parameters =>
            {
                if (!Context.CurrentUser.Claims.Contains("super"))
                    return new RedirectResponse("/");

                return View["Modules", new { SuperUser = Context.CurrentUser.Claims.Contains("super"), Modules = ModuleManager.GetModules(), Errors = ModuleManager.LoadExceptions }];
            };

            Get["/modules/edit/{module}"] = p_Parameters =>
            {
                if (!Context.CurrentUser.Claims.Contains("super"))
                    return new RedirectResponse("/");

                String s_ModuleName = p_Parameters.module;

                var s_Module = ModuleManager.GetModule(s_ModuleName);

                if (s_Module == null)
                    return new RedirectResponse("/modules");

                return View["EditModule", new { SuperUser = Context.CurrentUser.Claims.Contains("super"), Data = s_Module, HasError = false, Error = "" }];
            };

            Post["/modules/edit/{module}"] = p_Parameters =>
            {
                if (!Context.CurrentUser.Claims.Contains("super"))
                    return new RedirectResponse("/");

                String s_ModuleName = p_Parameters.module;

                var s_Module = ModuleManager.GetModule(s_ModuleName);

                if (s_Module == null)
                    return new RedirectResponse("/modules");

                var s_Request = this.Bind<EditModuleRequest>();

                s_Module.DisplayName = s_Request.Display;
                s_Module.Description = s_Request.Description;
                s_Module.Script = s_Request.Script;

                if (String.IsNullOrWhiteSpace(s_Request.Display) ||
                    String.IsNullOrWhiteSpace(s_Request.Script))
                    return View["EditModule", new { SuperUser = Context.CurrentUser.Claims.Contains("super"), Data = s_Module, HasError = true, Error = "Please fill in all the required fields." }];

                ModuleManager.UpdateModule(s_Module);
                ModuleManager.ReloadModules();

                return new RedirectResponse("/modules");
            };

            Get["/modules/add"] = p_Parameters =>
            {
                if (!Context.CurrentUser.Claims.Contains("super"))
                    return new RedirectResponse("/");

                return View["AddModule", new { HasError = false, SuperUser = Context.CurrentUser.Claims.Contains("super"), Data = new AddModuleRequest() { Name = "", Display = "", Description = "", Script = "" }, Error = "" }];
            };

            Post["/modules/add"] = p_Parameters =>
            {
                if (!Context.CurrentUser.Claims.Contains("super"))
                    return new RedirectResponse("/");

                var s_Request = this.Bind<AddModuleRequest>();

                if (String.IsNullOrWhiteSpace(s_Request.Name.Trim()) || String.IsNullOrWhiteSpace(s_Request.Display) ||
                    String.IsNullOrWhiteSpace(s_Request.Script))
                    return View["AddModule", new { SuperUser = Context.CurrentUser.Claims.Contains("super"), Data = s_Request, HasError = true, Error = "Please fill in all the required fields." }];

                var s_ID = s_Request.Name.Trim().ToLowerInvariant();

                if (ModuleManager.GetModule(s_ID) != null)
                    return View["AddModule", new { SuperUser = Context.CurrentUser.Claims.Contains("super"), Data = s_Request, HasError = true, Error = "A module with that name already exists." }];

                var s_Module = new GrooveModule
                {
                    Name = s_ID,
                    DisplayName = s_Request.Display,
                    Description = s_Request.Description,
                    Script = s_Request.Script,
                    Enabled = true,
                    Default = false
                };

                using (var s_Db = Database.GetConnection())
                    s_Db.Insert(s_Module);

                ModuleManager.ReloadModules();

                return new RedirectResponse("/modules");
            };

            Get["/modules/delete/{module}"] = p_Parameters =>
            {
                if (!Context.CurrentUser.Claims.Contains("super"))
                    return new RedirectResponse("/");

                String s_ModuleName = p_Parameters.module;

                var s_Module = ModuleManager.GetModule(s_ModuleName);

                if (s_Module == null || s_Module.Default)
                    return new RedirectResponse("/modules");

                ModuleManager.RemoveModule(s_Module);
                ModuleManager.ReloadModules();

                return new RedirectResponse("/modules");
            };

            Get["/modules/disable/{module}"] = p_Parameters =>
            {
                if (!Context.CurrentUser.Claims.Contains("super"))
                    return new RedirectResponse("/");

                String s_ModuleName = p_Parameters.module;

                var s_Module = ModuleManager.GetModule(s_ModuleName);

                if (s_Module == null)
                    return new RedirectResponse("/modules");

                ModuleManager.DisableModule(s_Module);
                ModuleManager.ReloadModules();

                return new RedirectResponse("/modules");
            };

            Get["/modules/enable/{module}"] = p_Parameters =>
            {
                if (!Context.CurrentUser.Claims.Contains("super"))
                    return new RedirectResponse("/");

                String s_ModuleName = p_Parameters.module;

                var s_Module = ModuleManager.GetModule(s_ModuleName);

                if (s_Module == null)
                    return new RedirectResponse("/modules");

                ModuleManager.EnableModule(s_Module);
                ModuleManager.ReloadModules();

                return new RedirectResponse("/modules");
            };
        }
    }
}
