using System.Collections.Generic;
using Nancy;
using Nancy.Authentication.Forms;
using Nancy.Bootstrapper;
using Nancy.TinyIoc;
using Nancy.ViewEngines.SuperSimpleViewEngine;

namespace GrooveCaster.Nancy
{
    public class Bootstrapper : DefaultNancyBootstrapper
    {
        protected override void ApplicationStartup(TinyIoCContainer p_Container, IPipelines p_Pipelines)
        {
            FormsAuthentication.Enable(p_Pipelines, new FormsAuthenticationConfiguration()
            {
                RedirectUrl = "~/login",
                UserMapper = p_Container.Resolve<IUserMapper>()
            });
        }

        protected override void ConfigureApplicationContainer(TinyIoCContainer p_Container)
        {
            base.ConfigureApplicationContainer(p_Container);

            p_Container.Register<IEnumerable<ISuperSimpleViewEngineMatcher>>((c, p) => new List<ISuperSimpleViewEngineMatcher> { new GrooveCasterMatcher() });
        }
    }
}