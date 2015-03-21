using System.Collections.Generic;
using System.Data;
using GrooveCaster.Models;
using ServiceStack.OrmLite;

namespace GrooveCaster
{
    internal static partial class Migrations
    {
        private static void RunMigrations1300(IDbConnection p_Connection)
        {
            var s_Modules = new List<GrooveModule>()
            {
                {
                    new GrooveModule
                    {
                        Name = "find",
                        DisplayName = "Find",
                        Description = "Finds songs from the queue matching the specified name.",
                        Script = @"from GS.Lib.Events import ChatMessageEvent

def OnFind(p_Event, p_Data):
    s_SpecialGuest = BroadcastManager.GetGuestForUserID(p_Event.UserID)

    if not BroadcastManager.CanUseCommands(s_SpecialGuest):
        ChatManager.SendChatMessage(""Sorry %s, but you don't have permission to use this feature."" % p_Event.UserName)
        return

    if len(p_Data.strip()) == 0:
        ChatManager.SendChatMessage(""Usage: %sfind <name>"" % SettingsManager.CommandPrefix())
        return

    s_Index = QueueManager.GetPlayingSongIndex()

    s_MatchedSongs = []

    s_Queue = QueueManager.GetCurrentQueue()

    for i in range(s_Index + 1, len(s_Queue)):
        if p_Data.lower() in s_Queue[i].SongName.lower():
            s_MatchedSongs.append('%s • %s' % (s_Queue[i].SongName, s_Queue[i].ArtistName))

    if len(s_MatchedSongs) == 0:
        ChatManager.SendChatMessage(""No matching songs found."")
        return

    ChatManager.SendChatMessage(""Found songs: %s"" % (' | '.join(s_MatchedSongs)))

ChatManager.RegisterCommand('find', ' <name>: Finds songs in the queue whose names match <name>.', Action[ChatMessageEvent, object](OnFind))

def OnUnload():
    ChatManager.RemoveCommand('find')",
                        Enabled = true,
                        Default = true
                    }
                },
                {
                    new GrooveModule
                    {
                        Name = "playlists",
                        DisplayName = "Playlists",
                        Description = "Provides playlist management options via the Chat interface.",
                        Script = @"from GS.Lib.Events import ChatMessageEvent

def OnFindPlaylist(p_Event, p_Data):
    s_SpecialGuest = BroadcastManager.GetGuestForUserID(p_Event.UserID)

    if not BroadcastManager.CanUseCommands(s_SpecialGuest) or not s_SpecialGuest.SuperGuest:
        ChatManager.SendChatMessage(""Sorry %s, but you don't have permission to use this feature."" % p_Event.UserName)
        return

    if len(p_Data.strip()) == 0:
        ChatManager.SendChatMessage(""Usage: %sfindPlaylist <name>"" % SettingsManager.CommandPrefix())
        return

    s_Playlists = PlaylistManager.GetPlaylists()

    s_MatchedPlaylists = []

    for i in range(0, len(s_Playlists)):
        if p_Data.lower() in s_Playlists[i].Name.lower():
            s_MatchedPlaylists.append('#%d: %s' % (s_Playlists[i].ID, s_Playlists[i].Name))

    if len(s_MatchedPlaylists) == 0:
        ChatManager.SendChatMessage(""No matching playlists found."")
        return

    ChatManager.SendChatMessage(""Found playlists: %s"" % (' | '.join(s_MatchedPlaylists)))


def OnLoadPlaylist(p_Event, p_Data):
    s_SpecialGuest = BroadcastManager.GetGuestForUserID(p_Event.UserID)

    if not BroadcastManager.CanUseCommands(s_SpecialGuest) or not s_SpecialGuest.SuperGuest:
        ChatManager.SendChatMessage(""Sorry %s, but you don't have permission to use this feature."" % p_Event.UserName)
        return

    if len(p_Data.strip()) == 0 or not p_Data.strip().isdigit():
        ChatManager.SendChatMessage(""Usage: %sloadPlaylist <id>"" % SettingsManager.CommandPrefix())
        return

    s_Playlist = PlaylistManager.GetPlaylist(int(p_Data.strip()))

    if s_Playlist == None:
        ChatManager.SendChatMessage(""Could not find a playlist with the specified ID."")
        return

    if not PlaylistManager.LoadPlaylist(s_Playlist.ID, False):
        ChatManager.SendChatMessage(""Failed to load playlist '%s'."" % s_Playlist.Name)
        return

    ChatManager.SendChatMessage(""Successfully loaded playlist '%s'."" % s_Playlist.Name)


def OnQueuePlaylist(p_Event, p_Data):
    s_SpecialGuest = BroadcastManager.GetGuestForUserID(p_Event.UserID)

    if not BroadcastManager.CanUseCommands(s_SpecialGuest) or not s_SpecialGuest.SuperGuest:
        ChatManager.SendChatMessage(""Sorry %s, but you don't have permission to use this feature."" % p_Event.UserName)
        return

    if len(p_Data.strip()) == 0 or not p_Data.strip().isdigit():
        ChatManager.SendChatMessage(""Usage: %squeuePlaylist <id>"" % SettingsManager.CommandPrefix())
        return

    if not PlaylistManager.PlaylistActive or PlaylistManager.ActivePlaylist == None:
        ChatManager.SendChatMessage(""There is currently no active playlist. Please load a playlist first."")
        return

    s_Playlist = PlaylistManager.GetPlaylist(int(p_Data.strip()))

    if s_Playlist == None:
        ChatManager.SendChatMessage(""Could not find a playlist with the specified ID."")
        return

    if not PlaylistManager.QueuePlaylist(s_Playlist.ID, False):
        ChatManager.SendChatMessage(""Failed to queue playlist '%s'."" % s_Playlist.Name)
        return

    ChatManager.SendChatMessage(""Successfully queue playlist '%s'."" % s_Playlist.Name)


def OnDisablePlaylist(p_Event, p_Data):
    s_SpecialGuest = BroadcastManager.GetGuestForUserID(p_Event.UserID)

    if not BroadcastManager.CanUseCommands(s_SpecialGuest) or not s_SpecialGuest.SuperGuest:
        ChatManager.SendChatMessage(""Sorry %s, but you don't have permission to use this feature."" % p_Event.UserName)
        return

    if not PlaylistManager.PlaylistActive or PlaylistManager.ActivePlaylist == None:
        ChatManager.SendChatMessage(""There is currently no active playlist."")
        return

    PlaylistManager.DisablePlaylist()

    ChatManager.SendChatMessage(""Successfully disabled the active playlist."")


def OnPlaylist(p_Event, p_Data):
    s_SpecialGuest = BroadcastManager.GetGuestForUserID(p_Event.UserID)

    if not BroadcastManager.CanUseCommands(s_SpecialGuest):
        ChatManager.SendChatMessage(""Sorry %s, but you don't have permission to use this feature."" % p_Event.UserName)
        return

    if not PlaylistManager.PlaylistActive or PlaylistManager.ActivePlaylist == None:
        ChatManager.SendChatMessage(""There is currently no active playlist."")
        return

    ChatManager.SendChatMessage(""Now playing from playlist '%s'."" % PlaylistManager.ActivePlaylist.Name)


ChatManager.RegisterCommand('findPlaylist', ' <name>: Finds playlists whose name match <name> and displays their IDs.', Action[ChatMessageEvent, object](OnFindPlaylist))
ChatManager.RegisterCommand('loadPlaylist', ' <id>: Loads a playlist with the specified <id> into the queue.', Action[ChatMessageEvent, object](OnLoadPlaylist))
ChatManager.RegisterCommand('queuePlaylist', ' <id>: Queues the playlist with the specified <id> after the currently active playlist.', Action[ChatMessageEvent, object](OnQueuePlaylist))
ChatManager.RegisterCommand('disablePlaylist', ': Disables the currently active playlist.', Action[ChatMessageEvent, object](OnDisablePlaylist))
ChatManager.RegisterCommand('playlist', ': Displays the name of the currently active playlist (if any).', Action[ChatMessageEvent, object](OnPlaylist))

def OnUnload():
    ChatManager.RemoveCommand('findPlaylist')
    ChatManager.RemoveCommand('loadPlaylist')
    ChatManager.RemoveCommand('queuePlaylist')
    ChatManager.RemoveCommand('disablePlaylist')
    ChatManager.RemoveCommand('playlist')",
                        Enabled = true,
                        Default = true
                    }
                },
            };

            p_Connection.SaveAll(s_Modules);
        }
    }
}
