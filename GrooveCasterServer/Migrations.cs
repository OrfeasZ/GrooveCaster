using System;
using System.Data;
using GrooveCaster.Models;
using ServiceStack.OrmLite;

namespace GrooveCaster
{
    internal static class Migrations
    {
        internal static void RunMigrations(IDbConnection p_Connection, String p_DatabaseVersion)
        {
            switch (p_DatabaseVersion)
            {
                case "1.0.1.0":
                    RunMigrations1100(p_Connection);
                    break;

                case "1.0.0.0":
                    RunMigrations1010(p_Connection);
                    RunMigrations1100(p_Connection);
                    break;
            }

            var s_Version = p_Connection.SingleById<CoreSetting>("gcver");
            s_Version.Value = Program.GetVersion();

            p_Connection.Update(s_Version);
        }

        private static void RunMigrations1010(IDbConnection p_Connection)
        {
            // nothing here
        }

        private static void RunMigrations1100(IDbConnection p_Connection)
        {
            // nothing here
        }
    }
}
