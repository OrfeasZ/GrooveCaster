using Nancy;
using Nancy.Authentication.Forms;
using Nancy.Bootstrapper;
using Nancy.TinyIoc;

namespace GrooveCasterServer.Nancy
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
    }
}