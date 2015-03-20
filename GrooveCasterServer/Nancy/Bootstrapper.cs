using System;
using System.Collections.Generic;
using GrooveCaster.Models;
using Nancy;
using Nancy.Authentication.Forms;
using Nancy.Bootstrapper;
using Nancy.Responses;
using Nancy.TinyIoc;
using Nancy.ViewEngines.SuperSimpleViewEngine;
using ServiceStack.OrmLite;

namespace GrooveCaster.Nancy
{
    public class Bootstrapper : DefaultNancyBootstrapper
    {
        protected override void ApplicationStartup(TinyIoCContainer p_Container, IPipelines p_Pipelines)
        {
            Console.WriteLine("Bootstrapping SharpShark library...");

            // Bootstrap SharpShark library.
            var s_Setup = Application.BootstrapLibrary();

            // Has the user setup the bot?
            if (!s_Setup)
            {
                Console.WriteLine();
                Console.WriteLine("It looks like GrooveCaster has not been set up yet.");
                Console.WriteLine("Visit the web interface from a web browser, login with the username and password \"admin\", and follow the on-screen instructions in order to fully setup GrooveCaster.");
                Console.WriteLine();
            }

            FormsAuthentication.Enable(p_Pipelines, new FormsAuthenticationConfiguration()
            {
                RedirectUrl = "~/login",
                UserMapper = p_Container.Resolve<IUserMapper>()
            });

            p_Pipelines.BeforeRequest += (p_Context) =>
            {
                if (p_Context.CurrentUser == null)
                    return null;

                if (p_Context.Request.Url.Path.StartsWith("/setup") ||
                    p_Context.Request.Url.Path.StartsWith("/logout") ||
                    p_Context.Request.Url.Path.StartsWith("/licenses"))
                    return null;

                using (var s_Db = Database.GetConnection())
                {
                    var s_GSUsername = s_Db.SingleById<CoreSetting>("gsun");
                    var s_GSPassword = s_Db.SingleById<CoreSetting>("gspw");

                    if (s_GSUsername == null || s_GSPassword == null)
                        return new RedirectResponse("/setup");
                }

                return null;
            };
        }

        protected override void ConfigureApplicationContainer(TinyIoCContainer p_Container)
        {
            base.ConfigureApplicationContainer(p_Container);

            p_Container.Register<IEnumerable<ISuperSimpleViewEngineMatcher>>((c, p) => new List<ISuperSimpleViewEngineMatcher> { new GrooveCasterMatcher() });
        }
    }
}