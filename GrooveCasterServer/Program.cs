using System;
using System.Data;
using System.Diagnostics;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using GrooveCaster.Managers;
using GrooveCaster.Models;
using GS.Lib;
using GS.SneakyBeaky;
using Nancy;
using Nancy.Hosting.Self;
using NDesk.Options;
using Newtonsoft.Json;
using ServiceStack.OrmLite;

namespace GrooveCaster
{
    public class Program
    {
        public static SharpShark Library { get; set; }

        internal static NancyHost Host { get; set; }

        internal static String DbConnectionString { get; set; }

        internal static String SecretKey { get; set; }

        public static String LatestVersion { get; set; }

        private static bool m_Daemonize;

        private static String m_Host;

        private static bool m_ShowHelp;

        private static OptionSet m_Options;

        internal static void Main(string[] p_Args)
        {
            Console.Title = "GrooveCaster " + GetVersion();

            // Default values.
            m_Daemonize = false;
            m_Host = "http://localhost:42278";
            m_ShowHelp = false;

            m_Options = new OptionSet()
            {
                {
                    "b|bindhost=", "The host GrooveCaster will bind to. Defaults to \"http://localhost:42278\".",
                    v => m_Host = v
                },
                {
                    "d|daemon", "Run GrooveCaster as a daemon. Useful for when running with mono.",
                    v => m_Daemonize = v != null
                },
                {
                    "h|help", "Show this message and exit GrooveCaster.",
                    v => m_ShowHelp = v != null
                }
            };

            // Try to parse command-line options.
            try
            {
                m_Options.Parse(p_Args);
            }
            catch (OptionException s_Exception)
            {
                Console.Write("GrooveCaster: ");
                Console.WriteLine(s_Exception.Message);
                Console.WriteLine("Try `GrooveCaster --help' for more information.");
                return;
            }

            // If the user requested help, show him help!
            if (m_ShowHelp)
            {
                ShowHelp();
                return;
            }

            Uri s_HostUri;

            try
            {
                s_HostUri = new Uri(m_Host);
            }
            catch
            {
                Console.WriteLine("GrooveCaster: The host URI you provided is not valid.");
                Console.WriteLine("Try `GrooveCaster --help' for more information.");
                return;
            }

            Console.WriteLine("GrooveCaster is initializing. Please wait...");

            // Initialize local database.
            InitDatabase();

            Console.WriteLine("Fetching latest Secret Key from GrooveShark...");

            // Fetch secret keys.
            SecretKey = Beakynator.FetchSecretKey();
            Library = new SharpShark(SecretKey);

            // Fetch latest GrooveCaster version.
            LatestVersion = GetLatestVersion();

            // Check for updates every hour.
            var s_VersionCheckTimer = new System.Timers.Timer()
            {
                Interval = 3600000,
                AutoReset = true
            };

            s_VersionCheckTimer.Elapsed += (p_Sender, p_EventArgs) =>
            {
                LatestVersion = GetLatestVersion();
            };

            s_VersionCheckTimer.Start();

            // Enable error traces.
            StaticConfiguration.DisableErrorTraces = false;

            // Start Nancy host.
            using (Host = new NancyHost(new HostConfiguration()
            {
                UrlReservations = new UrlReservations()                 
                {
                    CreateAutomatically = true
                }
            }, s_HostUri))
            {
                Console.WriteLine("Starting web host...");

                try
                {
                    Host.Start();
                }
                catch (AutomaticUrlReservationCreationFailureException s_Exception)
                {
                    Console.WriteLine(s_Exception.Message);
                    Console.WriteLine("Press any key to exit.");
                    Console.ReadKey();
                    return;
                }

                Console.WriteLine("Bootstrapping SharpShark library...");

                // Bootstrap SharpShark library.
                var s_Setup = BootstrapLibrary();

                Console.WriteLine("GrooveCaster is active and running on " + s_HostUri);

                if (NeedsUpdate())
                {
                    Console.WriteLine();
                    Console.WriteLine();
                    Console.WriteLine("Your version of GrooveCaster is out of date (Current: {0}, Latest: {1}).", GetVersion(), LatestVersion);
                    Console.WriteLine("Visit http://orfeasz.github.io/GrooveCaster/ for instructions on how to update.");
                    Console.WriteLine();
                }

                // Has the user setup the bot?
                if (!s_Setup)
                {
                    Console.WriteLine();
                    Console.WriteLine("It looks like GrooveCaster has not been set up yet.");
                    Console.WriteLine("Visit " + s_HostUri + " from a web browser, login with the username and password \"admin\", and follow the on-screen instructions in order to fully setup GrooveCaster.");
                }

                // Should we daemonize this?
                if (m_Daemonize)
                {
                    Thread.Sleep(Timeout.Infinite);
                }
                else
                {
                    Console.WriteLine();
                    Console.WriteLine("Press any key to exit.");
                    Console.ReadKey();
                }

                Host.Stop();
            }
        }

        private static void ShowHelp()
        {
            Console.WriteLine("Usage: GrooveCaster [options]");
            Console.WriteLine();
            Console.WriteLine("Options:");
            m_Options.WriteOptionDescriptions(Console.Out);
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
                    SetupBaseSettings(s_Db);
                }

                if (!s_Db.TableExists<SpecialGuest>())
                {
                    s_Db.CreateTable<SpecialGuest>();
                }

                if (!s_Db.TableExists<SongEntry>())
                {
                    s_Db.CreateTable<SongEntry>();
                }

                if (!s_Db.TableExists<GrooveModule>())
                {
                    s_Db.CreateTable<GrooveModule>();
                }
            }
        }

        private static void SetupAdminUser(IDbConnection p_Connection)
        {
            var s_AdminUser = new AdminUser()
            {
                UserID = Guid.NewGuid(),
                Username = "admin",
                Superuser = true
            };

            using (SHA256 s_Sha1 = new SHA256Managed())
            {
                var s_HashBytes = s_Sha1.ComputeHash(Encoding.ASCII.GetBytes("admin"));
                s_AdminUser.Password = BitConverter.ToString(s_HashBytes).Replace("-", "").ToLowerInvariant();
            }

            p_Connection.Insert(s_AdminUser);
        }

        private static void SetupBaseSettings(IDbConnection p_Connection)
        {
            // Store current version; used for migrations.
            p_Connection.Insert(new CoreSetting() { Key = "gcver", Value = GetVersion() });
        }

        internal static bool BootstrapLibrary()
        {
            if (String.IsNullOrWhiteSpace(SecretKey))
                return true;

            BroadcastManager.Init();
            ChatManager.Init();
            QueueManager.Init();
            SettingsManager.Init();
            UserManager.Init();
            ModuleManager.Init();

            using (var s_Db = DbConnectionString.OpenDbConnection())
                if (s_Db.SingleById<CoreSetting>("gsun") == null || s_Db.SingleById<CoreSetting>("gspw") == null)
                    return false;
            
            UserManager.Authenticate();
            return true;
        }

        public static String GetVersion()
        {
            return FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location).FileVersion;
        }

        public static String GetLatestVersion()
        {
            try
            {
                var s_VersionData = new System.Net.WebClient().DownloadString("http://orfeasz.github.io/GrooveCaster/version.json");
                var s_Version = JsonConvert.DeserializeObject<VersionModel>(s_VersionData);
                return s_Version.Version;
            }
            catch
            {
                return GetVersion();
            }
        }

        public static bool NeedsUpdate()
        {
            var s_CurrentVersion = new Version(GetVersion());
            var s_LatestVersion = new Version(LatestVersion);

            var s_Result = s_CurrentVersion.CompareTo(s_LatestVersion);
            return s_Result < 0;
        }
    }
}
