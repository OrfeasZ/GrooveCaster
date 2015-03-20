using System;
using System.Diagnostics;
using System.Threading;
using Nancy.Hosting.Self;
using NDesk.Options;
using NLog;
using NLog.Config;
using NLog.Targets;

namespace GrooveCaster
{
    internal class Program
    {
        internal static NancyHost Host { get; set; }

        private static bool m_Daemonize;

        private static String m_Host;

        private static bool m_ShowHelp;

        private static bool m_Verbose;

        private static OptionSet m_Options;

        static void Main(string[] p_Args)
        {
            Console.Title = "GrooveCaster " + Application.GetVersion();

            // Default values.
            m_Daemonize = false;
            m_Host = "http://localhost:42278";
            m_ShowHelp = false;
            m_Verbose = false;

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
                },
                {
                    "v|verbose", "Enables verbose debugging output.",
                    v => m_Verbose = v != null
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

            if (m_Verbose)
                EnableVerboseOutput();

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
            Console.WriteLine("Fetching latest Secret Key from GrooveShark...");

            Application.SetSelfHosted();
            Application.FetchKey();
            Application.Init();

            // Start Nancy host.
            using (Host = new NancyHost(new HostConfiguration()
            {
                UrlReservations = new UrlReservations()
                {
                    CreateAutomatically = true
                }
            }, s_HostUri))
            {
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

                Console.WriteLine("GrooveCaster is active and running on " + s_HostUri);

                if (Application.NeedsUpdate())
                {
                    Console.WriteLine();
                    Console.WriteLine();
                    Console.WriteLine("Your version of GrooveCaster is out of date (Current: {0}, Latest: {1}).", Application.GetVersion(), Application.LatestVersion);
                    Console.WriteLine("Visit http://orfeasz.github.io/GrooveCaster/ for instructions on how to update.");
                    Console.WriteLine();
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

        private static void InitLogging()
        {
            var s_Config = new LoggingConfiguration();

            var s_ConsoleTarget = new ColoredConsoleTarget();
            s_Config.AddTarget("console", s_ConsoleTarget);

            s_ConsoleTarget.Layout = @"[${date:format=HH\:mm\:ss.fff}] ${logger} >> ${message}";

            var s_ConsoleRule = new LoggingRule("*", LogLevel.Trace, s_ConsoleTarget);
            s_Config.LoggingRules.Add(s_ConsoleRule);

            var s_FileTarget = new FileTarget();
            s_Config.AddTarget("file", s_FileTarget);

            s_FileTarget.FileName = "${basedir}/GrooveCaster.log";
            s_FileTarget.Layout = @"[${date:format=HH\:mm\:ss.fff}] ${logger} >> ${message}";
            s_FileTarget.ArchiveFileName = "${basedir}/GrooveCaster.{#}.log";
            s_FileTarget.ArchiveEvery = FileArchivePeriod.Day;
            s_FileTarget.ArchiveNumbering = ArchiveNumberingMode.Date;
            s_FileTarget.ArchiveDateFormat = "yyyMMdd";

            var s_FileRule = new LoggingRule("*", LogLevel.Trace, s_FileTarget);
            s_Config.LoggingRules.Add(s_FileRule);

            LogManager.Configuration = s_Config;
        }

        private static void EnableVerboseOutput()
        {
            InitLogging();

            LogManager.GetLogger("GrooveCaster").Info("Starting GrooveCaster at {0}.", DateTime.UtcNow);

            var s_Listener = new NLogTraceListener();
            Trace.Listeners.Add(s_Listener);
        }
    }
}
