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

        private static List<ChatMessageEvent> m_ChatHistory; 

        static ChatManager()
        {
            
        }

        internal static void Init()
        {
            m_ChatCommands = new Dictionary<string, ChatCommand>();
            m_ChatHistory = new List<ChatMessageEvent>();

            Program.Library.RegisterEventHandler(ClientEvent.ChatMessage, OnChatMessage);

            // Register internal commands.
            RegisterCommandInternal("about", ": Displays information about the GrooveCaster bot.", OnAbout);
            RegisterCommandInternal("help", " [command]: Displays detailed information about the command [command]. Displays all available commands if [command] is not specified.", OnHelp);
        }

        public static List<ChatMessageEvent> GetChatHistory()
        {
            return m_ChatHistory;
        }

        private static void OnChatMessage(SharkEvent p_SharkEvent)
        {
            var s_Event = p_SharkEvent as ChatMessageEvent;

            // Record the last 20 messages in chat history.
            m_ChatHistory.Add(s_Event);

            for (var i = 0; i < m_ChatHistory.Count - 20; ++i)
                m_ChatHistory.RemoveAt(0);

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

        private static void OnAbout(ChatMessageEvent p_Event, String p_Data)
        {
            SendChatMessage("This broadcast is powered by GrooveCaster " + Program.GetVersion() + ". For more information visit http://orfeasz.github.io/GrooveCaster/.");
        }

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
