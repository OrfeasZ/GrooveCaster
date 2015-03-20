using System;
using System.Diagnostics;
using System.Net;
using System.Reflection;
using System.Timers;
using GrooveCaster.Managers;
using GrooveCaster.Models;
using GS.Lib;
using GS.SneakyBeaky;
using Nancy;
using Newtonsoft.Json;
using ServiceStack.OrmLite;

namespace GrooveCaster
{
    public class Application
    {
        public static SharpShark Library { get; set; }

        internal static String SecretKey { get; set; }

        public static String LatestVersion { get; set; }

        internal static bool SelfHosted { get; private set; }

        static Application()
        {
            SelfHosted = false;
        }

        public static void SetSelfHosted()
        {
            SelfHosted = true;
        }

        public static void Init()
        {
            if (!SelfHosted)
                FetchKey();

            // Initialize local database.
            Database.Init();

            // Fetch latest GrooveCaster version.
            LatestVersion = GetLatestVersion();

            // Check for updates every hour.
            var s_VersionCheckTimer = new Timer()
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
        }

        public static void FetchKey()
        {
            // Fetch secret keys.
            SecretKey = Beakynator.FetchSecretKey();
            Library = new SharpShark(SecretKey);
        }

        internal static bool BootstrapLibrary()
        {
            if (String.IsNullOrWhiteSpace(SecretKey))
                return true;

            BroadcastManager.Init();
            ChatManager.Init();
            PlaylistManager.Init();
            QueueManager.Init();
            SettingsManager.Init();
            UserManager.Init();
            SuggestionManager.Init();
            StatisticsManager.Init();

            // ModuleManager should always load last.
            ModuleManager.Init();

            using (var s_Db = Database.GetConnection())
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
                var s_VersionData = new WebClient().DownloadString("http://orfeasz.github.io/GrooveCaster/version.json");
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
