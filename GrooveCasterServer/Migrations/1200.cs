using System;
using System.Collections.Generic;
using System.Data;
using GrooveCaster.Models;
using ServiceStack.OrmLite;

namespace GrooveCaster
{
    internal static partial class Migrations
    {
        private static void RunMigrations1200(IDbConnection p_Connection)
        {
            var s_ModulesToUpdate = new List<String>()
            {
                "remove-next", "remove-last", "fetch-by-name", "fetch-last", "remove-by-name",
                "skip", "shuffle", "make-guest", "peek", "queue-random", "add-guests", 
                "remove-guest", "unguest", "set-title", "set-description", "add-to-collection",
                "remove-from-collection", "seek"
            };

            var s_Modules = p_Connection.SelectByIds<GrooveModule>(s_ModulesToUpdate);

            foreach (var s_Module in s_Modules)
                s_Module.Script = s_Module.Script.Replace("if s_SpecialGuest == None",
                    "if not BroadcastManager.CanUseCommands(s_SpecialGuest)");

            p_Connection.SaveAll(s_Modules);
        }
    }
}
