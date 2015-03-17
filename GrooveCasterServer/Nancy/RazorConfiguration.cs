using System.Collections.Generic;
using Nancy.ViewEngines.Razor;

namespace GrooveCaster.Nancy
{
    public class RazorConfiguration : IRazorConfiguration
    {
        public IEnumerable<string> GetAssemblyNames()
        {
            yield return "GrooveCaster";
            yield return "ServiceStack.Text";
            yield return "GS.Lib";
            yield return "GS.SneakyBeaky";
        }

        public IEnumerable<string> GetDefaultNamespaces()
        {
            yield return "Nancy.Validation";
            yield return "System.Globalization";
            yield return "System.Collections.Generic";
            yield return "System.Linq";
            yield return "GrooveCaster";
            yield return "GrooveCaster.Modules";
            yield return "GrooveCaster.Models";
            yield return "GrooveCaster.Nancy";
        }

        public bool AutoIncludeModelNamespace
        {
            get { return true; }
        }
    }
}
