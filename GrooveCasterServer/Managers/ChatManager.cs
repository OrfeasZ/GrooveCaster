using System;
using System.Collections.Generic;
using System.Diagnostics;
using GrooveCasterServer.Models;
using GS.Lib.Enums;
using GS.Lib.Events;
using GS.Lib.Models;
using ServiceStack.OrmLite;

namespace GrooveCasterServer.Managers
{
    public static class ChatManager
    {
        static ChatManager()
        {
            
        }

        public static void Init()
        {
            Program.Library.RegisterEventHandler(ClientEvent.ChatMessage, OnChatMessage);
        }

        private static void OnChatMessage(SharkEvent p_SharkEvent)
        {
            var s_Event = p_SharkEvent as ChatMessageEvent;

            // Disregard messages that are sent by the bot.
            if (s_Event.UserID == Program.Library.User.Data.UserID)
                return;

            Debug.WriteLine("[CHAT] {0}: {1}", s_Event.UserName, s_Event.ChatMessage);

            var s_Parts = s_Event.ChatMessage.Split(' ');

            var s_Command = s_Parts[0];
            var s_Data = s_Event.ChatMessage.Substring(s_Command.Length).Trim();

            switch (s_Command)
            {
                case "!guest":
                    OnGuest(s_Event, s_Data);
                    break;

                case "!ping":
                    OnPing(s_Event, s_Data);
                    break;

                case "!removeNext":
                    OnRemoveNext(s_Event, s_Data);
                    break;

                case "!removeLast":
                    OnRemoveLast(s_Event, s_Data);
                    break;

                case "!fetchByName":
                    OnFetchByName(s_Event, s_Data);
                    break;

                case "!fetchLast":
                    OnFetchLast(s_Event, s_Data);
                    break;

                case "!removeByName":
                    OnRemoveByName(s_Event, s_Data);
                    break;

                case "!skip":
                    OnSkip(s_Event, s_Data);
                    break;

                case "!shuffle":
                    OnShuffle(s_Event, s_Data);
                    break;

                case "!makeGuest":
                    OnMakeGuest(s_Event, s_Data);
                    break;

                case "!addGuest":
                    OnAddGuest(s_Event, s_Data);
                    break;

                case "!removeGuest":
                    OnRemoveGuest(s_Event, s_Data);
                    break;

                case "!unguest":
                    OnUnguest(s_Event, s_Data);
                    break;

                case "!setTitle":
                    OnSetTitle(s_Event, s_Data);
                    break;

                case "!setDescription":
                    OnSetDescription(s_Event, s_Data);
                    break;

                case "!peek":
                    OnPeek(s_Event, s_Data);
                    break;

                case "!addToCollection":
                    OnAddToCollection(s_Event, s_Data);
                    break;

                case "!removeFromCollection":
                    OnRemoveFromCollection(s_Event, s_Data);
                    break;

                case "!about":
                    OnAbout(s_Event, s_Data);
                    break;

                case "!help":
                    OnHelp(s_Event, s_Data);
                    break;
            }
        }

        private static void OnGuest(ChatMessageEvent p_Event, String p_Data)
        {
            UserManager.GuestUser(p_Event.UserID, p_Event.UserName);
        }

        private static void OnPing(ChatMessageEvent p_Event, String p_Data)
        {
            Program.Library.Chat.SendChatMessage("Pong! Hello " + p_Event.UserName + " (" + p_Event.UserID + ")!");
        }

        private static void OnRemoveNext(ChatMessageEvent p_Event, String p_Data)
        {
            var s_SpecialGuest = UserManager.GetGuestForUserID(p_Event.UserID);

            if (s_SpecialGuest == null)
            {
                Program.Library.Chat.SendChatMessage("Sorry " + p_Event.UserName + ", but you don't have permission to use this feature.");
                return;
            }

            if (String.IsNullOrWhiteSpace(p_Data.Trim()))
            {
                QueueManager.RemoveNext();
                return;
            }

            int s_Count;
            if (!Int32.TryParse(p_Data.Trim(), out s_Count))
            {
                Program.Library.Chat.SendChatMessage("Usage: !removeNext [count]");
                return;
            }

            QueueManager.RemoveNext(s_Count);
        }

        private static void OnRemoveLast(ChatMessageEvent p_Event, String p_Data)
        {
            var s_SpecialGuest = UserManager.GetGuestForUserID(p_Event.UserID);

            if (s_SpecialGuest == null)
            {
                Program.Library.Chat.SendChatMessage("Sorry " + p_Event.UserName + ", but you don't have permission to use this feature.");
                return;
            }

            if (String.IsNullOrWhiteSpace(p_Data.Trim()))
            {
                QueueManager.RemoveLast();
                return;
            }

            int s_Count;
            if (!Int32.TryParse(p_Data.Trim(), out s_Count))
            {
                Program.Library.Chat.SendChatMessage("Usage: !removeLast [count]");
                return;
            }

            QueueManager.RemoveLast(s_Count);
        }

        private static void OnFetchByName(ChatMessageEvent p_Event, String p_Data)
        {
            var s_SpecialGuest = UserManager.GetGuestForUserID(p_Event.UserID);

            if (s_SpecialGuest == null)
            {
                Program.Library.Chat.SendChatMessage("Sorry " + p_Event.UserName + ", but you don't have permission to use this feature.");
                return;
            }

            QueueManager.FetchByName(p_Data);
        }

        private static void OnFetchLast(ChatMessageEvent p_Event, String p_Data)
        {
            var s_SpecialGuest = UserManager.GetGuestForUserID(p_Event.UserID);

            if (s_SpecialGuest == null)
            {
                Program.Library.Chat.SendChatMessage("Sorry " + p_Event.UserName + ", but you don't have permission to use this feature.");
                return;
            }

            QueueManager.FetchLast();
        }

        private static void OnRemoveByName(ChatMessageEvent p_Event, String p_Data)
        {
            var s_SpecialGuest = UserManager.GetGuestForUserID(p_Event.UserID);

            if (s_SpecialGuest == null)
            {
                Program.Library.Chat.SendChatMessage("Sorry " + p_Event.UserName + ", but you don't have permission to use this feature.");
                return;
            }

            QueueManager.RemoveByName(p_Data.Trim());
        }

        private static void OnSkip(ChatMessageEvent p_Event, String p_Data)
        {
            var s_SpecialGuest = UserManager.GetGuestForUserID(p_Event.UserID);

            if (s_SpecialGuest == null)
            {
                Program.Library.Chat.SendChatMessage("Sorry " + p_Event.UserName + ", but you don't have permission to use this feature.");
                return;
            }

            QueueManager.SkipSong();
        }

        private static void OnShuffle(ChatMessageEvent p_Event, String p_Data)
        {
            var s_SpecialGuest = UserManager.GetGuestForUserID(p_Event.UserID);

            if (s_SpecialGuest == null)
            {
                Program.Library.Chat.SendChatMessage("Sorry " + p_Event.UserName + ", but you don't have permission to use this feature.");
                return;
            }

            QueueManager.Shuffle();
        }

        private static void OnMakeGuest(ChatMessageEvent p_Event, String p_Data)
        {
            var s_SpecialGuest = UserManager.GetGuestForUserID(p_Event.UserID);

            if (s_SpecialGuest == null || !s_SpecialGuest.CanAddTemporaryGuests)
            {
                Program.Library.Chat.SendChatMessage("Sorry " + p_Event.UserName + ", but you don't have permission to use this feature.");
                return;
            }

            var s_Parts = p_Data.Split(' ');

            if (s_Parts.Length != 1)
            {
                Program.Library.Chat.SendChatMessage("Usage: !makeGuest <userID>");
                return;
            }

            Int64 s_UserID;
            if (!Int64.TryParse(s_Parts[0], out s_UserID))
            {
                 Program.Library.Chat.SendChatMessage("Usage: !makeGuest <userID>");
                return;
            }

            if (Program.Library.Broadcast.SpecialGuests.Contains(s_UserID))
                return;

            Program.Library.Broadcast.AddSpecialGuest(s_UserID, VIPPermissions.Suggestions);
        }

        private static void OnAddGuest(ChatMessageEvent p_Event, String p_Data)
        {
            var s_SpecialGuest = UserManager.GetGuestForUserID(p_Event.UserID);

            if (s_SpecialGuest == null || !s_SpecialGuest.CanAddPermanentGuests)
            {
                Program.Library.Chat.SendChatMessage("Sorry " + p_Event.UserName + ", but you don't have permission to use this feature.");
                return;
            }

            var s_Parts = p_Data.Split(' ');

            if (s_Parts.Length != 2)
            {
                Program.Library.Chat.SendChatMessage("Usage: !addGuest <userID> <userName>");
                return;
            }

            Int64 s_UserID;
            if (!Int64.TryParse(s_Parts[0], out s_UserID) || String.IsNullOrWhiteSpace(s_Parts[1]))
            {
                Program.Library.Chat.SendChatMessage("Usage: !addGuest <userID> <userName>");
                return;
            }

            using (var s_Db = Program.DbConnectionString.OpenDbConnection())
            {
                var s_Guest = s_Db.SingleById<SpecialGuest>(s_UserID);

                if (s_Guest != null)
                {
                    Program.Library.Chat.SendChatMessage("The user you specified already has guest permissions.");
                    return;
                }

                s_Guest = new SpecialGuest()
                {
                    Username = s_Parts[1],
                    UserID = s_UserID,
                    Permissions = VIPPermissions.Suggestions | VIPPermissions.ChatModerate
                };

                s_Db.Insert(s_Guest);

                Program.Library.Chat.SendChatMessage("Successfully granted user '" + s_Parts[1] + "' guest permissions.");
            }
        }

        private static void OnRemoveGuest(ChatMessageEvent p_Event, String p_Data)
        {
            var s_SpecialGuest = UserManager.GetGuestForUserID(p_Event.UserID);

            if (s_SpecialGuest == null || !s_SpecialGuest.CanAddPermanentGuests)
            {
                Program.Library.Chat.SendChatMessage("Sorry " + p_Event.UserName + ", but you don't have permission to use this feature.");
                return;
            }

            var s_Parts = p_Data.Split(' ');

            if (s_Parts.Length != 1)
            {
                Program.Library.Chat.SendChatMessage("Usage: !removeGuest <userID|userName>");
                return;
            }

            if (String.IsNullOrWhiteSpace(s_Parts[0]))
            {
                Program.Library.Chat.SendChatMessage("Usage: !removeGuest <userID|userName>");
                return;
            }

            Int64 s_UserID = -1;
            Int64.TryParse(s_Parts[0], out s_UserID);

            using (var s_Db = Program.DbConnectionString.OpenDbConnection())
            {
                SpecialGuest s_Guest;
                
                if (s_UserID != -1)
                    s_Guest = s_Db.SingleById<SpecialGuest>(s_UserID);
                else
                    s_Guest = s_Db.Single<SpecialGuest>(p_Guest => p_Guest.Username.Equals(s_Parts[0], StringComparison.OrdinalIgnoreCase));

                if (s_Guest == null)
                {
                    Program.Library.Chat.SendChatMessage("The user you specified could not be found.");
                    return;
                }

                s_Db.Delete(s_Guest);

                Program.Library.Chat.SendChatMessage("Successfully revoked guest permissions from user '" + s_Guest.Username + "'.");
            }
        }

        private static void OnUnguest(ChatMessageEvent p_Event, String p_Data)
        {
            if (String.IsNullOrWhiteSpace(p_Data) && Program.Library.Broadcast.SpecialGuests.Contains(p_Event.UserID))
            {
                Program.Library.Broadcast.RemoveSpecialGuest(p_Event.UserID);
                return;
            }

            var s_SpecialGuest = UserManager.GetGuestForUserID(p_Event.UserID);

            if (s_SpecialGuest == null || !s_SpecialGuest.CanAddPermanentGuests)
            {
                Program.Library.Chat.SendChatMessage("Sorry " + p_Event.UserName + ", but you don't have permission to use this feature.");
                return;
            }

            var s_Parts = p_Data.Split(' ');

            if (s_Parts.Length > 1)
            {
                Program.Library.Chat.SendChatMessage("Usage: !unguest [userID]");
                return;
            }

            if (s_Parts.Length == 0 || String.IsNullOrWhiteSpace(s_Parts[0]))
            {
                UserManager.UnguestAll();
                return;
            }

            Int64 s_UserID;
            if (!Int64.TryParse(s_Parts[0], out s_UserID))
            {
                Program.Library.Chat.SendChatMessage("Usage: !unguest [userID]");
                return;
            }

            if (!Program.Library.Broadcast.SpecialGuests.Contains(s_UserID))
                return;

            Program.Library.Broadcast.RemoveSpecialGuest(s_UserID);
        }

        private static void OnSetTitle(ChatMessageEvent p_Event, String p_Data)
        {
            var s_SpecialGuest = UserManager.GetGuestForUserID(p_Event.UserID);

            if (s_SpecialGuest == null || !s_SpecialGuest.CanEditTitle)
            {
                Program.Library.Chat.SendChatMessage("Sorry " + p_Event.UserName + ", but you don't have permission to use this feature.");
                return;
            }

            if (p_Data.Trim().Length < 3)
            {
                Program.Library.Chat.SendChatMessage("Usage: !setTitle <title>");
                return;
            }

            Program.Library.Broadcast.UpdateBroadcastName(p_Data.Trim());
        }

        private static void OnSetDescription(ChatMessageEvent p_Event, String p_Data)
        {
            var s_SpecialGuest = UserManager.GetGuestForUserID(p_Event.UserID);

            if (s_SpecialGuest == null || !s_SpecialGuest.CanEditDescription)
            {
                Program.Library.Chat.SendChatMessage("Sorry " + p_Event.UserName + ", but you don't have permission to use this feature.");
                return;
            }

            if (p_Data.Trim().Length < 3)
            {
                Program.Library.Chat.SendChatMessage("Usage: !setDescription <description>");
                return;
            }

            Program.Library.Broadcast.UpdateBroadcastDescription(p_Data.Trim());
        }

        private static void OnPeek(ChatMessageEvent p_Event, String p_Data)
        {
            var s_SpecialGuest = UserManager.GetGuestForUserID(p_Event.UserID);

            if (s_SpecialGuest == null)
            {
                Program.Library.Chat.SendChatMessage("Sorry " + p_Event.UserName + ", but you don't have permission to use this feature.");
                return;
            }

            var s_Index = Program.Library.Queue.GetPlayingSongIndex();

            var s_UpcomingSongs = new List<QueueSongData>();

            // Limit peek to 6 songs.
            var s_SongCount = Program.Library.Queue.CurrentQueue.Count - s_Index - 1;

            if (s_SongCount > 6)
                s_SongCount = 6;

            for (var i = s_Index + 1; i < s_Index + 1 + s_SongCount; ++i)
                s_UpcomingSongs.Add(Program.Library.Queue.CurrentQueue[i]);

            if (s_UpcomingSongs.Count == 0)
            {
                Program.Library.Chat.SendChatMessage("The are no upcoming songs in the queue.");
                return;
            }

            var s_Songs = "Upcoming Songs: ";

            for (var i = 0; i < s_UpcomingSongs.Count; ++i)
                s_Songs += String.Format("{0} • {1} | ", s_UpcomingSongs[i].SongName, s_UpcomingSongs[i].ArtistName);

            s_Songs = s_Songs.Substring(0, s_Songs.Length - 3);

            Program.Library.Chat.SendChatMessage(s_Songs);
        }

        private static void OnAddToCollection(ChatMessageEvent p_Event, String p_Data)
        {
            var s_SpecialGuest = UserManager.GetGuestForUserID(p_Event.UserID);

            if (s_SpecialGuest == null || !s_SpecialGuest.SuperGuest)
            {
                Program.Library.Chat.SendChatMessage("Sorry " + p_Event.UserName + ", but you don't have permission to use this feature.");
                return;
            }

            using (var s_Db = Program.DbConnectionString.OpenDbConnection())
            {
                var s_Song = s_Db.SingleById<SongEntry>(Program.Library.Broadcast.PlayingSongID);

                if (s_Song != null)
                {
                    Program.Library.Chat.SendChatMessage("Song already exists in the collection.");
                    return;
                }

                s_Song = new SongEntry()
                {
                    AlbumID = Program.Library.Broadcast.PlayingAlbumID,
                    AlbumName = Program.Library.Broadcast.PlayingSongAlbum,
                    ArtistID = Program.Library.Broadcast.PlayingArtistID,
                    ArtistName = Program.Library.Broadcast.PlayingSongArtist,
                    SongID = Program.Library.Broadcast.PlayingSongID,
                    SongName = Program.Library.Broadcast.PlayingSongName
                };

                s_Db.Insert(s_Song);
                QueueManager.CollectionSongs.Add(s_Song.SongID);

                Program.Library.Chat.SendChatMessage("Song has been successfully added to the collection.");
            }
        }

        private static void OnRemoveFromCollection(ChatMessageEvent p_Event, String p_Data)
        {
            var s_SpecialGuest = UserManager.GetGuestForUserID(p_Event.UserID);

            if (s_SpecialGuest == null || !s_SpecialGuest.SuperGuest)
            {
                Program.Library.Chat.SendChatMessage("Sorry " + p_Event.UserName + ", but you don't have permission to use this feature.");
                return;
            }

            using (var s_Db = Program.DbConnectionString.OpenDbConnection())
            {
                var s_Song = s_Db.SingleById<SongEntry>(Program.Library.Broadcast.PlayingSongID);

                if (s_Song == null)
                {
                    Program.Library.Chat.SendChatMessage("Song does not exist in the collection.");
                    return;
                }

                QueueManager.CollectionSongs.Remove(s_Song.SongID);
                s_Db.Delete(s_Song);

                Program.Library.Chat.SendChatMessage("Song has been successfully removed from the collection.");
            }
        }

        private static void OnAbout(ChatMessageEvent p_Event, String p_Data)
        {
            Program.Library.Chat.SendChatMessage("This broadcast is powered by GrooveCaster " + Program.GetVersion() + ". For more information visit http://orfeasz.github.io/GrooveCaster/.");
        }

        private static readonly Dictionary<String, String> m_CommandHelp = new Dictionary<string, string>()
        {
            { "guest", "!guest: Toggle special guest status." },
            { "ping", "!ping: Ping the GrooveCaster server." },
            { "removeNext", "!removeNext [count]: Removes the next [count] songs from the queue ([count] defaults to 1 if not specified)." },
            { "removeLast", "!removeLast [count]: Removes the last [count] songs from the queue ([count] defaults to 1 if not specified)." },
            { "fetchByName", "!fetchByName <name>: Fetches a song from the queue with a name matching <name> and moves it after the playing song." },
            { "fetchLast", "!fetchLast: Fetches the last song in the queue and moves it after the playing song." },
            { "removeByName", "!removeByName <name>: Removes all songs whose name matches <name> from the queue." },
            { "skip", "!skip: Skips the current song." },
            { "shuffle", "!shuffle: Shuffles the songs in the queue." },
            { "peek", "!peek: Displays a list of upcoming songs from the queue." },
            { "makeGuest", "!makeGuest <userid>: Makes user with user ID <userid> a temporary special guest." },
            { "addGuest", "!addGuest <userid>: Makes user with user ID <userid> a permanent special guest." },
            { "removeGuest", "!removeGuest <userid>: Permanently removes special guest permissions from user with user ID <userid>." },
            { "unguest", "!unguest [userid]: Temporarily removes special guest permissions from user with user ID [userid]. Unguests everyone if [userid] is not specified." },
            { "addToCollection", "!addToCollection: Adds the currently playing song to the song collection." },
            { "removeFromCollection", "!removeFromCollection: Removes the currently playing song from the song collection." },
            { "setTitle", "!setTitle <title>: Sets the title of the broadcast." },
            { "setDescription", "!setDescription <description>: Sets the description of the broadcast." },
            { "about", "!about: Displays information about the GrooveCaster bot." },
            { "help", "!help [command]: Displays detailed information about the command [command]. Displays all available commands if [command] is not specified." },
        }; 

        private static void OnHelp(ChatMessageEvent p_Event, String p_Data)
        {
            var s_Command = p_Data.Trim();

            if (String.IsNullOrWhiteSpace(s_Command))
            {
                var s_Commands = "Available commands: ";

                foreach (var s_Pair in m_CommandHelp)
                    s_Commands += s_Pair.Key + " ";

                Program.Library.Chat.SendChatMessage(s_Commands);
                Program.Library.Chat.SendChatMessage("For detailed information on a command use !help [command].");
                return;
            }

            if (!m_CommandHelp.ContainsKey(s_Command))
            {
                Program.Library.Chat.SendChatMessage("Command not found.");
                return;
            }

            Program.Library.Chat.SendChatMessage(m_CommandHelp[s_Command]);
        }
    }
}
