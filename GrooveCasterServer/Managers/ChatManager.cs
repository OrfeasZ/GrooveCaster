using System;
using System.Collections.Generic;
using System.Diagnostics;
using GrooveCaster.Models;
using GS.Lib.Enums;
using GS.Lib.Events;
using GS.Lib.Models;

namespace GrooveCaster.Managers
{
    public static class ChatManager
    {
        private static Dictionary<String, ChatCommand> m_ChatCommands; 

        static ChatManager()
        {
            
        }

        internal static void Init()
        {
            m_ChatCommands = new Dictionary<string, ChatCommand>();
            Program.Library.RegisterEventHandler(ClientEvent.ChatMessage, OnChatMessage);

            RegisterCommandInternal("about", ": Displays information about the GrooveCaster bot.", OnAbout);
            RegisterCommandInternal("help", " [command]: Displays detailed information about the command [command]. Displays all available commands if [command] is not specified.", OnHelp);
        }

        private static void OnChatMessage(SharkEvent p_SharkEvent)
        {
            var s_Event = p_SharkEvent as ChatMessageEvent;

            // Disregard messages that are sent by the bot.
            if (s_Event.UserID == Program.Library.User.Data.UserID)
                return;

            Debug.WriteLine("[CHAT] {0}: {1}", s_Event.UserName, s_Event.ChatMessage);

            if (s_Event.ChatMessage.Trim().Length < 2 || s_Event.ChatMessage.Trim()[0] != SettingsManager.CommandPrefix())
                return;

            var s_Parts = s_Event.ChatMessage.Trim().Split(' ');

            var s_Command = s_Parts[0];
            var s_Data = s_Event.ChatMessage.Substring(s_Command.Length).Trim();

            ChatCommand s_ChatCommand;
            if (!m_ChatCommands.TryGetValue(s_Command.Substring(1), out s_ChatCommand))
                return;

            s_ChatCommand.Callback(s_Event, s_Data);
        }
        
        private static void OnPing(ChatMessageEvent p_Event, String p_Data)
        {
            SendChatMessage("Pong! Hello " + p_Event.UserName + " (" + p_Event.UserID + ")!");
        }

        private static void OnRemoveNext(ChatMessageEvent p_Event, String p_Data)
        {
            var s_SpecialGuest = BroadcastManager.GetGuestForUserID(p_Event.UserID);

            if (s_SpecialGuest == null)
            {
                SendChatMessage("Sorry " + p_Event.UserName + ", but you don't have permission to use this feature.");
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
                SendChatMessage("Usage: !removeNext [count]");
                return;
            }

            QueueManager.RemoveNext(s_Count);
        }

        private static void OnRemoveLast(ChatMessageEvent p_Event, String p_Data)
        {
            var s_SpecialGuest = BroadcastManager.GetGuestForUserID(p_Event.UserID);

            if (s_SpecialGuest == null)
            {
                SendChatMessage("Sorry " + p_Event.UserName + ", but you don't have permission to use this feature.");
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
                SendChatMessage("Usage: !removeLast [count]");
                return;
            }

            QueueManager.RemoveLast(s_Count);
        }

        private static void OnFetchByName(ChatMessageEvent p_Event, String p_Data)
        {
            var s_SpecialGuest = BroadcastManager.GetGuestForUserID(p_Event.UserID);

            if (s_SpecialGuest == null)
            {
                SendChatMessage("Sorry " + p_Event.UserName + ", but you don't have permission to use this feature.");
                return;
            }

            QueueManager.FetchByName(p_Data);
        }

        private static void OnFetchLast(ChatMessageEvent p_Event, String p_Data)
        {
            var s_SpecialGuest = BroadcastManager.GetGuestForUserID(p_Event.UserID);

            if (s_SpecialGuest == null)
            {
                SendChatMessage("Sorry " + p_Event.UserName + ", but you don't have permission to use this feature.");
                return;
            }

            QueueManager.FetchLast();
        }

        private static void OnRemoveByName(ChatMessageEvent p_Event, String p_Data)
        {
            var s_SpecialGuest = BroadcastManager.GetGuestForUserID(p_Event.UserID);

            if (s_SpecialGuest == null)
            {
                SendChatMessage("Sorry " + p_Event.UserName + ", but you don't have permission to use this feature.");
                return;
            }

            QueueManager.RemoveByName(p_Data.Trim());
        }

        private static void OnSkip(ChatMessageEvent p_Event, String p_Data)
        {
            var s_SpecialGuest = BroadcastManager.GetGuestForUserID(p_Event.UserID);

            if (s_SpecialGuest == null)
            {
                SendChatMessage("Sorry " + p_Event.UserName + ", but you don't have permission to use this feature.");
                return;
            }

            QueueManager.SkipSong();
        }

        private static void OnShuffle(ChatMessageEvent p_Event, String p_Data)
        {
            var s_SpecialGuest = BroadcastManager.GetGuestForUserID(p_Event.UserID);

            if (s_SpecialGuest == null)
            {
                SendChatMessage("Sorry " + p_Event.UserName + ", but you don't have permission to use this feature.");
                return;
            }

            QueueManager.Shuffle();
        }

        private static void OnMakeGuest(ChatMessageEvent p_Event, String p_Data)
        {
            var s_SpecialGuest = BroadcastManager.GetGuestForUserID(p_Event.UserID);

            if (s_SpecialGuest == null || !s_SpecialGuest.CanAddTemporaryGuests)
            {
                SendChatMessage("Sorry " + p_Event.UserName + ", but you don't have permission to use this feature.");
                return;
            }

            var s_Parts = p_Data.Split(' ');

            if (s_Parts.Length != 1)
            {
                SendChatMessage("Usage: !makeGuest <userID>");
                return;
            }

            Int64 s_UserID;
            if (!Int64.TryParse(s_Parts[0], out s_UserID))
            {
                 SendChatMessage("Usage: !makeGuest <userID>");
                return;
            }

            if (Program.Library.Broadcast.SpecialGuests.Contains(s_UserID))
                return;

            Program.Library.Broadcast.AddSpecialGuest(s_UserID, VIPPermissions.Suggestions);
        }

        private static void OnAddGuest(ChatMessageEvent p_Event, String p_Data)
        {
            var s_SpecialGuest = BroadcastManager.GetGuestForUserID(p_Event.UserID);

            if (s_SpecialGuest == null || !s_SpecialGuest.CanAddPermanentGuests)
            {
                SendChatMessage("Sorry " + p_Event.UserName + ", but you don't have permission to use this feature.");
                return;
            }

            var s_Parts = p_Data.Split(' ');

            if (s_Parts.Length != 2)
            {
                SendChatMessage("Usage: !addGuest <userID> <userName>");
                return;
            }

            Int64 s_UserID;
            if (!Int64.TryParse(s_Parts[0], out s_UserID) || String.IsNullOrWhiteSpace(s_Parts[1]))
            {
                SendChatMessage("Usage: !addGuest <userID> <userName>");
                return;
            }

            if (!BroadcastManager.AddGuest(s_Parts[1], s_UserID, VIPPermissions.Suggestions | VIPPermissions.ChatModerate))
            {
                SendChatMessage("The user you specified already has guest permissions.");
                return;
            }

            SendChatMessage("Successfully granted user '" + s_Parts[1] + "' guest permissions.");
        }

        private static void OnRemoveGuest(ChatMessageEvent p_Event, String p_Data)
        {
            var s_SpecialGuest = BroadcastManager.GetGuestForUserID(p_Event.UserID);

            if (s_SpecialGuest == null || !s_SpecialGuest.CanAddPermanentGuests)
            {
                SendChatMessage("Sorry " + p_Event.UserName + ", but you don't have permission to use this feature.");
                return;
            }

            var s_Parts = p_Data.Split(' ');

            if (s_Parts.Length != 1)
            {
                SendChatMessage("Usage: !removeGuest <userID|userName>");
                return;
            }

            if (String.IsNullOrWhiteSpace(s_Parts[0]))
            {
                SendChatMessage("Usage: !removeGuest <userID|userName>");
                return;
            }

            Int64 s_UserID = -1;
            Int64.TryParse(s_Parts[0], out s_UserID);

            SpecialGuest s_Guest;
            if (!BroadcastManager.RemoveGuest(s_UserID, out s_Guest))
            {
                SendChatMessage("The user you specified could not be found.");
                return;
            }

            SendChatMessage("Successfully revoked guest permissions from user '" + s_Guest.Username + "'.");
        }

        private static void OnUnguest(ChatMessageEvent p_Event, String p_Data)
        {
            var s_SpecialGuest = BroadcastManager.GetGuestForUserID(p_Event.UserID);

            if (s_SpecialGuest == null || !s_SpecialGuest.CanAddPermanentGuests)
            {
                SendChatMessage("Sorry " + p_Event.UserName + ", but you don't have permission to use this feature.");
                return;
            }

            var s_Parts = p_Data.Split(' ');

            if (s_Parts.Length > 1)
            {
                SendChatMessage("Usage: !unguest [userID]");
                return;
            }

            if (s_Parts.Length == 0 || String.IsNullOrWhiteSpace(s_Parts[0]))
            {
                BroadcastManager.UnguestAll();
                return;
            }

            Int64 s_UserID;
            if (!Int64.TryParse(s_Parts[0], out s_UserID))
            {
                SendChatMessage("Usage: !unguest [userID]");
                return;
            }

            if (!Program.Library.Broadcast.SpecialGuests.Contains(s_UserID))
                return;

            Program.Library.Broadcast.RemoveSpecialGuest(s_UserID);
        }

        private static void OnSetTitle(ChatMessageEvent p_Event, String p_Data)
        {
            var s_SpecialGuest = BroadcastManager.GetGuestForUserID(p_Event.UserID);

            if (s_SpecialGuest == null || !s_SpecialGuest.CanEditTitle)
            {
                SendChatMessage("Sorry " + p_Event.UserName + ", but you don't have permission to use this feature.");
                return;
            }

            if (p_Data.Trim().Length < 3)
            {
                SendChatMessage("Usage: !setTitle <title>");
                return;
            }

            Program.Library.Broadcast.UpdateBroadcastName(p_Data.Trim());
        }

        private static void OnSetDescription(ChatMessageEvent p_Event, String p_Data)
        {
            var s_SpecialGuest = BroadcastManager.GetGuestForUserID(p_Event.UserID);

            if (s_SpecialGuest == null || !s_SpecialGuest.CanEditDescription)
            {
                SendChatMessage("Sorry " + p_Event.UserName + ", but you don't have permission to use this feature.");
                return;
            }

            if (p_Data.Trim().Length < 3)
            {
                SendChatMessage("Usage: !setDescription <description>");
                return;
            }

            Program.Library.Broadcast.UpdateBroadcastDescription(p_Data.Trim());
        }

        private static void OnPeek(ChatMessageEvent p_Event, String p_Data)
        {
            var s_SpecialGuest = BroadcastManager.GetGuestForUserID(p_Event.UserID);

            if (s_SpecialGuest == null)
            {
                SendChatMessage("Sorry " + p_Event.UserName + ", but you don't have permission to use this feature.");
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
                SendChatMessage("The are no upcoming songs in the queue.");
                return;
            }

            var s_Songs = "Upcoming Songs: ";

            for (var i = 0; i < s_UpcomingSongs.Count; ++i)
                s_Songs += String.Format("{0} • {1} | ", s_UpcomingSongs[i].SongName, s_UpcomingSongs[i].ArtistName);

            s_Songs = s_Songs.Substring(0, s_Songs.Length - 3);

            SendChatMessage(s_Songs);
        }

        private static void OnAddToCollection(ChatMessageEvent p_Event, String p_Data)
        {
            var s_SpecialGuest = BroadcastManager.GetGuestForUserID(p_Event.UserID);

            if (s_SpecialGuest == null || !s_SpecialGuest.SuperGuest)
            {
                SendChatMessage("Sorry " + p_Event.UserName + ", but you don't have permission to use this feature.");
                return;
            }

            if (!QueueManager.AddPlayingSongToCollection())
            {
                SendChatMessage("Song already exists in the collection.");
                return;
            }

            SendChatMessage("Song has been successfully added to the collection.");
        }

        private static void OnRemoveFromCollection(ChatMessageEvent p_Event, String p_Data)
        {
            var s_SpecialGuest = BroadcastManager.GetGuestForUserID(p_Event.UserID);

            if (s_SpecialGuest == null || !s_SpecialGuest.SuperGuest)
            {
                SendChatMessage("Sorry " + p_Event.UserName + ", but you don't have permission to use this feature.");
                return;
            }

            if (!QueueManager.RemovePlayingSongFromCollection())
            {
                SendChatMessage("Song does not exist in the collection.");
                return;
            }
                
            SendChatMessage("Song has been successfully removed from the collection.");
        }

        private static void OnAbout(ChatMessageEvent p_Event, String p_Data)
        {
            SendChatMessage("This broadcast is powered by GrooveCaster " + Program.GetVersion() + ". For more information visit http://orfeasz.github.io/GrooveCaster/.");
        }

        private static void OnSeek(ChatMessageEvent p_Event, String p_Data)
        {
            var s_SpecialGuest = BroadcastManager.GetGuestForUserID(p_Event.UserID);

            if (s_SpecialGuest == null || !s_SpecialGuest.SuperGuest)
            {
                SendChatMessage("Sorry " + p_Event.UserName + ", but you don't have permission to use this feature.");
                return;
            }

            Int64 s_Seconds;
            if (!Int64.TryParse(p_Data, out s_Seconds) || s_Seconds < 0)
            {
                SendChatMessage("Usage: !seek <seconds>");
                return;
            }

            Program.Library.Broadcast.SeekCurrentSong(s_Seconds * 1000.0);
        }

        private static void OnQueueRandom(ChatMessageEvent p_Event, String p_Data)
        {
             var s_SpecialGuest = BroadcastManager.GetGuestForUserID(p_Event.UserID);

            if (s_SpecialGuest == null)
            {
                SendChatMessage("Sorry " + p_Event.UserName + ", but you don't have permission to use this feature.");
                return;
            }

            if (String.IsNullOrWhiteSpace(p_Data))
            {
                QueueManager.QueueRandomSongs(1);
                return;
            }

            Int32 s_Songs;
            if (!Int32.TryParse(p_Data, out s_Songs) || s_Songs <= 0)
            {
                SendChatMessage("Usage: !queueRandom [count]");
                return;
            }

            QueueManager.QueueRandomSongs(s_Songs);
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
            { "queueRandom", "!queueRandom [count]: Adds [count] random songs to the end of the queue ([count] defaults to 1 if not specified)." },
            { "skip", "!skip: Skips the current song." },
            { "shuffle", "!shuffle: Shuffles the songs in the queue." },
            { "peek", "!peek: Displays a list of upcoming songs from the queue." },
            { "makeGuest", "!makeGuest <userid>: Makes user with user ID <userid> a temporary special guest." },
            { "addGuest", "!addGuest <userid>: Makes user with user ID <userid> a permanent special guest." },
            { "removeGuest", "!removeGuest <userid>: Permanently removes special guest permissions from user with user ID <userid>." },
            { "unguest", "!unguest [userid]: Temporarily removes special guest permissions from user with user ID [userid]. Unguests everyone if [userid] is not specified." },
            { "addToCollection", "!addToCollection: Adds the currently playing song to the song collection." },
            { "removeFromCollection", "!removeFromCollection: Removes the currently playing song from the song collection." },
            { "seek", "!seek <second>: Seeks to the <second> second of the currently playing song." },
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

                foreach (var s_Pair in m_ChatCommands)
                {
                    if (!s_Pair.Value.MainInstance)
                        continue;

                    s_Commands += s_Pair.Value.Command + " • ";
                }

                SendChatMessage(s_Commands.Substring(0, s_Commands.Length - 3));
                SendChatMessage("For detailed information on a command use " + SettingsManager.CommandPrefix() + "help [command].");
                return;
            }

            ChatCommand s_ChatCommand;
            if (!m_ChatCommands.TryGetValue(s_Command, out s_ChatCommand))
            {
                SendChatMessage("Command not found.");
                return;
            }

            // Print command description.
            SendChatMessage(SettingsManager.CommandPrefix() + s_ChatCommand.Command + s_ChatCommand.Description);

            // Print command aliases.
            if (s_ChatCommand.Aliases.Count == 0)
                return;

            var s_Aliases = "Aliases: ";

            foreach (var s_Alias in s_ChatCommand.Aliases)
                s_Aliases += s_Alias + " • ";

            SendChatMessage(s_Aliases.Substring(0, s_Aliases.Length - 3));
        }

        public static void SendChatMessage(String p_Message)
        {
            Program.Library.Chat.SendChatMessage(p_Message);
        }

        private static void RegisterCommandInternal(String p_Command, String p_Description,
            Action<ChatMessageEvent, String> p_Callback, List<String> p_Aliases = null)
        {
            // Remove all commands with the same name.
            ChatCommand s_Command;
            if (m_ChatCommands.TryGetValue(p_Command, out s_Command))
            {
                m_ChatCommands.Remove(s_Command.Command);

                foreach (var s_Alias in s_Command.Aliases)
                    m_ChatCommands.Remove(s_Alias);
            }

            // Remove all commands with the same aliases.
            if (p_Aliases != null)
            {
                foreach (var s_Alias in p_Aliases)
                {
                    if (m_ChatCommands.TryGetValue(s_Alias, out s_Command))
                    {
                        m_ChatCommands.Remove(s_Command.Command);

                        foreach (var s_OtherAlias in s_Command.Aliases)
                            m_ChatCommands.Remove(s_OtherAlias);
                    }
                }
            }

            // Register the main command.
            s_Command = new ChatCommand()
            {
                Command = p_Command,
                Description = p_Description,
                Aliases = p_Aliases ?? new List<string>(),
                Callback = p_Callback,
                MainInstance = true
            };

            m_ChatCommands.Add(s_Command.Command, s_Command);

            // Register all the aliases.
            if (p_Aliases != null)
            {
                var s_SecondaryInstance = new ChatCommand()
                {
                    Command = p_Command,
                    Description = p_Description,
                    Aliases = p_Aliases,
                    Callback = p_Callback,
                    MainInstance = false
                };

                foreach (var s_Alias in p_Aliases)
                    m_ChatCommands.Add(s_Alias, s_SecondaryInstance);
            }
        }

        public static void RegisterCommand(String p_Command, String p_Description,
            Action<ChatMessageEvent, String> p_Callback, List<String> p_Aliases = null)
        {
            if (p_Command == "about" || p_Command == "help" ||
                (p_Aliases != null && (p_Aliases.Contains("about") && p_Aliases.Contains("help"))))
                return;

            RegisterCommandInternal(p_Command, p_Description, p_Callback, p_Aliases);
        }

        public static void RemoveCommand(String p_Command)
        {
            if (p_Command == "about" || p_Command == "help")
                return;

            // Remove all commands with the same name.
            ChatCommand s_Command;
            if (m_ChatCommands.TryGetValue(p_Command, out s_Command))
            {
                m_ChatCommands.Remove(s_Command.Command);

                foreach (var s_Alias in s_Command.Aliases)
                    m_ChatCommands.Remove(s_Alias);
            }
        }
    }
}
