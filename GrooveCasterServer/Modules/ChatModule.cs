using GrooveCaster.Managers;
using GrooveCaster.Models;
using Nancy;
using Nancy.ModelBinding;
using Nancy.Responses;
using Nancy.Security;

namespace GrooveCaster.Modules
{
    public class ChatModule : NancyModule
    {
        public ChatModule()
        {
            this.RequiresAuthentication();

            Get["/chat/history.json"] = p_Parameters =>
            {
                return ChatManager.GetChatHistory();
            };

            Post["/chat/send"] = p_Parameters =>
            {
                var s_Request = this.Bind<SendChatMessageRequest>();

                ChatManager.SendChatMessage(s_Request.Message);

                return new RedirectResponse("/");
            };
        }
    }
}
