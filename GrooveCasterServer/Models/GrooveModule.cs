using System;
using ServiceStack.DataAnnotations;

namespace GrooveCaster.Models
{
    public class GrooveModule
    {
        [PrimaryKey]
        public String Name { get; set; }

        public String DisplayName { get; set; }

        public String Description { get; set; }

        public String Script { get; set; }

        public bool Enabled { get; set; }

        public bool Default { get; set; }
    }
}
