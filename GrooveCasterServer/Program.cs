using System;
using System.Collections.Generic;
using System.Data;
using System.Security.Cryptography;
using System.Text;
using GrooveCasterServer.Managers;
using GrooveCasterServer.Models;
using GS.Lib;
using GS.Lib.Enums;
using GS.Lib.Events;
using GS.Lib.Models;
using GS.SneakyBeaky;
using Nancy.Hosting.Self;
using ServiceStack.OrmLite;

namespace GrooveCasterServer
{
    class Program
    {
        public static SharpShark Library { get; set; }

        public static NancyHost Host { get; set; }

        public static String DbConnectionString { get; set; }

        public static String SecretKey { get; set; }

        static void Main(string[] p_Args)
        {
            InitDatabase();

            SecretKey = Beakynator.FetchSecretKey();
            Library = new SharpShark(SecretKey);
            
            var s_Uri = new Uri("http://localhost:3579");

            BootstrapLibrary();

            using (Host = new NancyHost(s_Uri))
            {
                Host.Start();

                Console.WriteLine("Your application is running on " + s_Uri);
                Console.WriteLine("Press any [Enter] to close the host.");
                Console.ReadLine();
            }
        }

        private static void InitDatabase()
        {
            OrmLiteConfig.DialectProvider = SqliteDialect.Provider;
            DbConnectionString = "gcaster.sqlite";

            using (var s_Db = DbConnectionString.OpenDbConnection())
            {
                if (!s_Db.TableExists<AdminUser>())
                {
                    s_Db.CreateTable<AdminUser>();
                    SetupAdminUser(s_Db);
                }

                if (!s_Db.TableExists<CoreSetting>())
                {
                    s_Db.CreateTable<CoreSetting>();
                }

                if (!s_Db.TableExists<SpecialGuest>())
                {
                    s_Db.CreateTable<SpecialGuest>();
                }
            }
        }

        private static void SetupAdminUser(IDbConnection p_Connection)
        {
            var s_AdminUser = new AdminUser()
            {
                UserID = Guid.NewGuid(),
                Username = "admin"
            };

            using (SHA256 s_Sha1 = new SHA256Managed())
            {
                var s_HashBytes = s_Sha1.ComputeHash(Encoding.ASCII.GetBytes("admin"));
                s_AdminUser.Password = BitConverter.ToString(s_HashBytes).Replace("-", "").ToLowerInvariant();
            }

            p_Connection.Insert(s_AdminUser);
        }

        public static void BootstrapLibrary()
        {
            if (String.IsNullOrWhiteSpace(SecretKey))
                return;

            BroadcastManager.Init();
            ChatManager.Init();
            QueueManager.Init();
            SettingsManager.Init();
            UserManager.Init();

            using (var s_Db = DbConnectionString.OpenDbConnection())
                if (s_Db.SingleById<CoreSetting>("gsun") == null || s_Db.SingleById<CoreSetting>("gspw") == null)
                    return;
            
            UserManager.Authenticate();
        }
    }
}
