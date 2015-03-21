using Nancy.ViewEngines.SuperSimpleViewEngine;

namespace GrooveCaster.Nancy
{
    public class GrooveCasterMatcher : ISuperSimpleViewEngineMatcher
    {
        public string Invoke(string p_Content, dynamic p_Model, IViewEngineHost p_Host)
        {
            return p_Content.Replace("@GrooveCaster.Version", Application.GetVersion());
        }
    }
}
