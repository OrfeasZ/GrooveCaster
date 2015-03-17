using System;
using System.Collections.Generic;
using GS.Lib.Events;

namespace GrooveCaster.Models
{
    public class ChatCommand
    {
        public String Command { get; set; }

        public String Description { get; set; }
        
        public List<String> Aliases { get; set; }

        public Action<ChatMessageEvent, String> Callback { get; set; }

        public bool MainInstance { get; set; }
    }
}
