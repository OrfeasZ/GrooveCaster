using System;
using System.Collections.Generic;
using System.Data;
using System.Security.Cryptography;
using System.Text;
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

        public static List<Int64> CollectionSongs { get; set; } 
        public static List<Int64> PlayedSongs { get; set; }

        public static String DbConnectionString { get; set; }

        public static String SecretKey { get; set; }

        private const int c_MaxSongHistory = 10;

        static void Main(string[] p_Args)
        {
            InitDatabase();

            SecretKey = Beakynator.FetchSecretKey();
            Library = new SharpShark(SecretKey);

            /*

            if (Library.User.Authenticate("user", "pass") != AuthenticationResult.Success)
            {
                Console.WriteLine("Authentication failed. Exiting...");
                return;
            }

            Library.RegisterEventHandler(ClientEvent.Authenticated, OnAuthenticated);
            Library.RegisterEventHandler(ClientEvent.BroadcastCreationFailed, OnBroadcastCreationFailed);
            Library.RegisterEventHandler(ClientEvent.BroadcastCreated, OnBroadcastCreated);
            Library.RegisterEventHandler(ClientEvent.ChatMessage, OnChatMessage);
            Library.RegisterEventHandler(ClientEvent.SongPlaying, OnSongPlaying);*/

            var s_Uri = new Uri("http://localhost:3579");

            using (Host = new NancyHost(s_Uri))
            {
                //Library.Chat.Connect();

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

        private static void OnSongPlaying(SharkEvent p_SharkEvent)
        {
            var s_Event = (SongPlayingEvent) p_SharkEvent;

            Console.WriteLine("Currently playing song '{0}' ({1}).", s_Event.SongName, s_Event.SongID);

            PlayedSongs.Add(s_Event.SongID);

            for (var i = 0; i < PlayedSongs.Count - c_MaxSongHistory; ++i)
                PlayedSongs.RemoveAt(0);

            // We're running out of songs; add from collection.
            if (Library.Queue.GetInternalIndexForSong(s_Event.QueueID) == Library.Queue.CurrentQueue.Count - 1)
            {
                var s_Random = new Random();
                var s_RandomSongIndex = s_Random.Next(0, CollectionSongs.Count - 1);

                var s_SongID = CollectionSongs[s_RandomSongIndex];

                if (CollectionSongs.Count <= PlayedSongs.Count)
                    PlayedSongs.Clear();

                while (PlayedSongs.Contains(s_SongID))
                {
                    s_RandomSongIndex = s_Random.Next(0, CollectionSongs.Count - 1);
                    s_SongID = CollectionSongs[s_RandomSongIndex];
                }

                Console.WriteLine("Adding song with ID '{0}' to queue.", s_SongID);
                Library.Broadcast.AddSongs(new List<Int64> { s_SongID });
            }
        }

        private static void OnAuthenticated(SharkEvent p_Event)
        {
            Console.WriteLine("Successfully authenticated; creating broadcast.");
            Library.Broadcast.CreateBroadcast("Test Broadcast via Bot [rev3]", "This broadcast is created using SharpShark [rev3].", new CategoryTag(156, "electronic"));
        }

        private static void OnBroadcastCreationFailed(SharkEvent p_Event)
        {
            Console.WriteLine("Broadcast creation failed?!?");
        }

        private static void OnBroadcastCreated(SharkEvent p_Event)
        {
            Console.WriteLine("Broadcast successfully created! ({0})", (p_Event as BroadcastCreationEvent).BroadcastID);
            CollectionSongs = Library.User.GetCollectionSongs();
            PlayedSongs = new List<long>();

            if (CollectionSongs.Count == 0)
            {
                Console.WriteLine("No songs in collection. Add songs manually or broadcast will die.");
                return;
            }

            // Add two random song to the collection.
            var s_Random = new Random();
            var s_RandomSongIndex01 = s_Random.Next(0, CollectionSongs.Count - 1);
            var s_RandomSongIndex02 = s_Random.Next(0, CollectionSongs.Count - 1);

            var s_SongID01 = CollectionSongs[s_RandomSongIndex01];
            var s_SongID02 = CollectionSongs[s_RandomSongIndex02];

            while (s_SongID02 == s_SongID01)
            {
                s_RandomSongIndex02 = s_Random.Next(0, CollectionSongs.Count - 1);
                s_SongID02 = CollectionSongs[s_RandomSongIndex02];
            }

            Library.Broadcast.AddSongs(new List<Int64> { s_SongID01, s_SongID02 });
        }

        private static void OnChatMessage(SharkEvent p_Event)
        {
            var s_Event = p_Event as ChatMessageEvent;
            Console.WriteLine("[CHAT] {0}: {1}", s_Event.UserName, s_Event.ChatMessage);

            if (s_Event.ChatMessage == "!guest")
            {
                if (Library.Broadcast.SpecialGuests.Contains(s_Event.UserID))
                    Library.Broadcast.RemoveSpecialGuest(s_Event.UserID);
                else
                    Library.Broadcast.AddSpecialGuest(s_Event.UserID, VIPPermissions.ChatModerate | VIPPermissions.Suggestions);

                return;
            }

            if (s_Event.ChatMessage == "!ping")
            {
                Library.Chat.SendChatMessage("Pong! Hello " + s_Event.UserName + " (" + s_Event.UserID + ")!");
                return;
            }

            if (s_Event.ChatMessage.StartsWith("!desc"))
            {
                Library.Broadcast.UpdateBroadcastDescription(s_Event.ChatMessage.Replace("!desc ", ""));
                return;
            }

            if (s_Event.ChatMessage.StartsWith("!name"))
            {
                Library.Broadcast.UpdateBroadcastName(s_Event.ChatMessage.Replace("!name ", ""));
                return;
            }
        }
    }
}
