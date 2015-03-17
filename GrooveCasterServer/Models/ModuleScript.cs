using Microsoft.Scripting.Hosting;

namespace GrooveCaster.Models
{
    public class ModuleScript
    {
        public ScriptScope Scope { get; set; }
        public ScriptSource Source { get; set; }
        public dynamic Script { get; set; }
    }
}
