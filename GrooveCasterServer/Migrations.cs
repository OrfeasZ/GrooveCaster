using System;
using System.Data;
using GrooveCaster.Models;
using ServiceStack.OrmLite;

namespace GrooveCaster
{
    internal static partial class Migrations
    {
        internal static void RunMigrations(IDbConnection p_Connection, String p_DatabaseVersion)
        {
            switch (p_DatabaseVersion)
            {
                case "1.2.0.0":
                    RunMigrations1300(p_Connection);
                    break;

                case "1.1.2.1":
                    RunMigrations1200(p_Connection);
                    RunMigrations1300(p_Connection);
                    break;

                case "1.1.2.0":
                    RunMigrations1121(p_Connection);
                    RunMigrations1200(p_Connection);
                    RunMigrations1300(p_Connection);
                    break;

                case "1.1.0.0":
                    RunMigrations1120(p_Connection);
                    RunMigrations1121(p_Connection);
                    RunMigrations1200(p_Connection);
                    RunMigrations1300(p_Connection);
                    break;

                case "1.0.1.0":
                    RunMigrations1100(p_Connection);
                    RunMigrations1120(p_Connection);
                    RunMigrations1121(p_Connection);
                    RunMigrations1200(p_Connection);
                    RunMigrations1300(p_Connection);
                    break;

                case "1.0.0.0":
                    RunMigrations1010(p_Connection);
                    RunMigrations1100(p_Connection);
                    RunMigrations1120(p_Connection);
                    RunMigrations1121(p_Connection);
                    RunMigrations1200(p_Connection);
                    RunMigrations1300(p_Connection);
                    break;
            }

            var s_Version = p_Connection.SingleById<CoreSetting>("gcver");
            s_Version.Value = Application.GetVersion();

            p_Connection.Update(s_Version);
        }
    }
}
