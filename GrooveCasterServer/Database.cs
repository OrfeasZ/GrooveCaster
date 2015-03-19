using System;
using System.Collections.Generic;
using System.Data;
using System.Security.Cryptography;
using System.Text;
using GrooveCaster.Models;
using ServiceStack.OrmLite;

namespace GrooveCaster
{
    public static class Database
    {
        private static String m_DbConnectionString;

        public static IDbConnection GetConnection()
        {
            return m_DbConnectionString.OpenDbConnection();
        }

        internal static void Init()
        {
            OrmLiteConfig.DialectProvider = SqliteDialect.Provider;
            m_DbConnectionString = "gcaster.sqlite";

            using (var s_Db = GetConnection())
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
                    SetupDefaultModules(s_Db);
                }

                if (!s_Db.TableExists<Playlist>())
                {
                    s_Db.CreateTable<Playlist>();
                }

                if (!s_Db.TableExists<PlaylistEntry>())
                {
                    s_Db.CreateTable<PlaylistEntry>();
                }

                var s_DatabaseVersionString = s_Db.SingleById<CoreSetting>("gcver").Value;
                var s_DatabaseVersion = new Version(s_DatabaseVersionString);
                var s_CurrentVersion = new Version(Program.GetVersion());

                if (s_DatabaseVersion.CompareTo(s_CurrentVersion) < 0)
                    Migrations.RunMigrations(s_Db, s_DatabaseVersionString);
            }
        }

        internal static void SetupAdminUser(IDbConnection p_Connection)
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

        internal static void SetupBaseSettings(IDbConnection p_Connection)
        {
            // Store current version; used for migrations.
            p_Connection.Insert(new CoreSetting() { Key = "gcver", Value = Program.GetVersion() });
        }

        internal static void SetupDefaultModules(IDbConnection p_Connection)
        {
            var s_Modules = new List<GrooveModule>()
            {
                {
                    new GrooveModule
                    {
                        Name = "guest",
                        DisplayName = "Guest",
                        Description = "Allows listeners with Special Guest permissions to toggle their guest status.",
                        Script = @"from GS.Lib.Events import ChatMessageEvent

def OnGuest(p_Event, p_Data):
	if BroadcastManager.HasActiveGuest(p_Event.UserID):
		BroadcastManager.Unguest(p_Event.UserID)
		return

	s_SpecialGuest = BroadcastManager.GetGuestForUserID(p_Event.UserID)

	if s_SpecialGuest == None:
		ChatManager.SendChatMessage(""Sorry %s, but you don't have permission to use this feature."" % p_Event.UserName)
		return

	BroadcastManager.MakeGuest(s_SpecialGuest)

ChatManager.RegisterCommand('guest', ': Toggle special guest status.', Action[ChatMessageEvent, object](OnGuest))

def OnUnload():
	ChatManager.RemoveCommand('guest')",
                        Enabled = true,
                        Default = true
                    }
                },
                {
                    new GrooveModule
                    {
                        Name = "ping",
                        DisplayName = "Ping",
                        Description = "Utility function which allows users to ping the bot.",
                        Script = @"from GS.Lib.Events import ChatMessageEvent

def OnPing(p_Event, p_Data):
  ChatManager.SendChatMessage(""Pong! Hey %s, your User ID is %d!"" % (p_Event.UserName, p_Event.UserID))

ChatManager.RegisterCommand('ping', ': Ping the GrooveCaster server.', Action[ChatMessageEvent, object](OnPing))

def OnUnload():
	ChatManager.RemoveCommand('ping')",
                        Enabled = true,
                        Default = true
                    }
                },
                {
                    new GrooveModule
                    {
                        Name = "remove-next",
                        DisplayName = "Remove Next",
                        Description = "Removes the next N songs from the queue.",
                        Script = @"from GS.Lib.Events import ChatMessageEvent

def OnRemoveNext(p_Event, p_Data):
    s_SpecialGuest = BroadcastManager.GetGuestForUserID(p_Event.UserID)

    if s_SpecialGuest == None:
        ChatManager.SendChatMessage(""Sorry %s, but you don't have permission to use this feature."" % p_Event.UserName)
        return

    if len(p_Data.strip()) == 0:
        QueueManager.RemoveNext()
        return

    if not p_Data.strip().isdigit():
        ChatManager.SendChatMessage(""Usage: %sremoveNext [count]"" % SettingsManager.CommandPrefix())
        return

    QueueManager.RemoveNext(int(p_Data.strip()))

ChatManager.RegisterCommand('removeNext', ' [count]: Removes the next [count] songs from the queue ([count] defaults to 1 if not specified).', Action[ChatMessageEvent, object](OnRemoveNext))

def OnUnload():
    ChatManager.RemoveCommand('removeNext')",
                        Enabled = true,
                        Default = true
                    }
                },
                {
                    new GrooveModule
                    {
                        Name = "remove-last",
                        DisplayName = "Remove Last",
                        Description = "Removes the last N songs from the queue.",
                        Script = @"from GS.Lib.Events import ChatMessageEvent

def OnRemoveLast(p_Event, p_Data):
    s_SpecialGuest = BroadcastManager.GetGuestForUserID(p_Event.UserID)

    if s_SpecialGuest == None:
        ChatManager.SendChatMessage(""Sorry %s, but you don't have permission to use this feature."" % p_Event.UserName)
        return

    if len(p_Data.strip()) == 0:
        QueueManager.RemoveLast()
        return

    if not p_Data.strip().isdigit():
        ChatManager.SendChatMessage(""Usage: %sremoveLast [count]"" % SettingsManager.CommandPrefix())
        return

    QueueManager.RemoveLast(int(p_Data.strip()))

ChatManager.RegisterCommand('removeLast', ' [count]: Removes the last [count] songs from the queue ([count] defaults to 1 if not specified).', Action[ChatMessageEvent, object](OnRemoveLast))

def OnUnload():
    ChatManager.RemoveCommand('removeLast')",
                        Enabled = true,
                        Default = true
                    }
                },
                {
                    new GrooveModule
                    {
                        Name = "fetch-by name",
                        DisplayName = "Fetch by Name",
                        Description = "Fetches a song from the queue by name and moves it after the playing song.",
                        Script = @"from GS.Lib.Events import ChatMessageEvent

def OnFetchByName(p_Event, p_Data):
    s_SpecialGuest = BroadcastManager.GetGuestForUserID(p_Event.UserID)

    if s_SpecialGuest == None:
        ChatManager.SendChatMessage(""Sorry %s, but you don't have permission to use this feature."" % p_Event.UserName)
        return

    if len(p_Data.strip()) == 0:
        ChatManager.SendChatMessage(""Usage: %sfetchByName <name>"" % SettingsManager.CommandPrefix())
        return
      
    QueueManager.FetchByName(p_Data.strip())

ChatManager.RegisterCommand('fetchByName', ' <name>: Fetches a song from the queue with a name matching <name> and moves it after the playing song.', Action[ChatMessageEvent, object](OnFetchByName))

def OnUnload():
    ChatManager.RemoveCommand('fetchByName')",
                        Enabled = true,
                        Default = true
                    }
                },
                {
                    new GrooveModule
                    {
                        Name = "fetch-last",
                        DisplayName = "Fetch Last",
                        Description = "Fetches the last song from the queue and moves it after the playing song.",
                        Script = @"from GS.Lib.Events import ChatMessageEvent

def OnFetchLast(p_Event, p_Data):
    s_SpecialGuest = BroadcastManager.GetGuestForUserID(p_Event.UserID)

    if s_SpecialGuest == None:
        ChatManager.SendChatMessage(""Sorry %s, but you don't have permission to use this feature."" % p_Event.UserName)
        return

    QueueManager.FetchLast()

ChatManager.RegisterCommand('fetchLast', ': Fetches the last song in the queue and moves it after the playing song.', Action[ChatMessageEvent, object](OnFetchLast))

def OnUnload():
    ChatManager.RemoveCommand('fetchLast')",
                        Enabled = true,
                        Default = true
                    }
                },
                {
                    new GrooveModule
                    {
                        Name = "remove-by name",
                        DisplayName = "Remove by Name",
                        Description = "Removes all songs whose name matches the provided name from the queue.",
                        Script = @"from GS.Lib.Events import ChatMessageEvent

def OnRemoveByName(p_Event, p_Data):
    s_SpecialGuest = BroadcastManager.GetGuestForUserID(p_Event.UserID)

    if s_SpecialGuest == None:
        ChatManager.SendChatMessage(""Sorry %s, but you don't have permission to use this feature."" % p_Event.UserName)
        return

    if len(p_Data.strip()) == 0:
        ChatManager.SendChatMessage(""Usage: %sremoveByName <name>"" % SettingsManager.CommandPrefix())
        return

    QueueManager.RemoveByName(p_Data.strip())

ChatManager.RegisterCommand('removeByName', ' <name>: Removes all songs whose name matches <name> from the queue.', Action[ChatMessageEvent, object](OnRemoveByName))

def OnUnload():
    ChatManager.RemoveCommand('removeByName')",
                        Enabled = true,
                        Default = true
                    }
                },
                {
                    new GrooveModule
                    {
                        Name = "skip",
                        DisplayName = "Skip",
                        Description = "Skips the currently playing song.",
                        Script = @"from GS.Lib.Events import ChatMessageEvent

def OnSkip(p_Event, p_Data):
    s_SpecialGuest = BroadcastManager.GetGuestForUserID(p_Event.UserID)

    if s_SpecialGuest == None:
        ChatManager.SendChatMessage(""Sorry %s, but you don't have permission to use this feature."" % p_Event.UserName)
        return

    QueueManager.SkipSong()

ChatManager.RegisterCommand('skip', ': Skips the current song.', Action[ChatMessageEvent, object](OnSkip))

def OnUnload():
    ChatManager.RemoveCommand('skip')",
                        Enabled = true,
                        Default = true
                    }
                },
                {
                    new GrooveModule
                    {
                        Name = "shuffle",
                        DisplayName = "Shuffle",
                        Description = "Shuffles the songs in the queue.",
                        Script = @"from GS.Lib.Events import ChatMessageEvent

def OnShuffle(p_Event, p_Data):
    s_SpecialGuest = BroadcastManager.GetGuestForUserID(p_Event.UserID)

    if s_SpecialGuest == None:
        ChatManager.SendChatMessage(""Sorry %s, but you don't have permission to use this feature."" % p_Event.UserName)
        return

    QueueManager.Shuffle()

ChatManager.RegisterCommand('shuffle', ': Shuffles the songs in the queue.', Action[ChatMessageEvent, object](OnShuffle))

def OnUnload():
    ChatManager.RemoveCommand('shuffle')",
                        Enabled = true,
                        Default = true
                    }
                },
                {
                    new GrooveModule
                    {
                        Name = "make-guest",
                        DisplayName = "Make Guest",
                        Description = "Makes a user a temporary special guest.",
                        Script = @"from GS.Lib.Events import ChatMessageEvent
from GS.Lib.Enums import VIPPermissions

def OnMakeGuest(p_Event, p_Data):
    s_SpecialGuest = BroadcastManager.GetGuestForUserID(p_Event.UserID)

    if s_SpecialGuest == None or not s_SpecialGuest.CanAddTemporaryGuests:
        ChatManager.SendChatMessage(""Sorry %s, but you don't have permission to use this feature."" % p_Event.UserName)
        return

    if len(p_Data.strip()) == 0 or not p_Data.strip().isdigit():
        ChatManager.SendChatMessage(""Usage: %smakeGuest <userid>"" % SettingsManager.CommandPrefix())
        return

    if BroadcastManager.HasActiveGuest(p_Event.UserID):
        return
      
    BroadcastManager.MakeGuest(int(p_Data.strip()), VIPPermissions.Suggestions)

ChatManager.RegisterCommand('makeGuest', ' <userid>: Makes user with user ID <userid> a temporary special guest.', Action[ChatMessageEvent, object](OnMakeGuest))

def OnUnload():
    ChatManager.RemoveCommand('makeGuest')",
                        Enabled = true,
                        Default = true
                    }
                },
                {
                    new GrooveModule
                    {
                        Name = "peek",
                        DisplayName = "Peek",
                        Description = "Displays a list of upcoming songs in the queue.",
                        Script =
                            "from GS.Lib.Events import ChatMessageEvent\r\n\r\ndef OnPeek(p_Event, p_Data):\r\n    s_SpecialGuest = BroadcastManager.GetGuestForUserID(p_Event.UserID)\r\n\r\n    if s_SpecialGuest == None:\r\n        ChatManager.SendChatMessage(\"Sorry %s, but you don\'t have permission to use this feature.\" % p_Event.UserName)\r\n        return\r\n\r\n    s_MaxCount = 6\r\n\r\n    if p_Data.strip().replace(\"-\", \"\").isdigit():\r\n        s_MaxCount = int(p_Data.strip())\r\n    \r\n    if s_MaxCount <= 0:\r\n        ChatManager.SendChatMessage(\"You can only peek into the future, not the past, silly %s!\" % p_Event.UserName)\r\n        return\r\n\r\n    s_Index = QueueManager.GetPlayingSongIndex()\r\n    s_SongCount = len(QueueManager.GetCurrentQueue()) - s_Index - 1\r\n\r\n    if s_SongCount > s_MaxCount:\r\n        s_SongCount = s_MaxCount\r\n\r\n    s_UpcomingSongs = []\r\n\r\n    for i in range(s_Index + 1, s_Index + 1 + s_SongCount):\r\n        s_UpcomingSongs.append(QueueManager.GetCurrentQueue()[i])\r\n\r\n    if len(s_UpcomingSongs) == 0:\r\n        ChatManager.SendChatMessage(\'There are no upcoming songs in the queue.\')\r\n        return\r\n\r\n    s_Songs = \'Upcoming songs: \'\r\n\r\n    for i in range(0, len(s_UpcomingSongs)):\r\n        s_Songs += \'%s • %s | \' % (s_UpcomingSongs[i].SongName, s_UpcomingSongs[i].ArtistName)\r\n\r\n    ChatManager.SendChatMessage(s_Songs[:-3])\r\n\r\nChatManager.RegisterCommand(\'peek\', \' [count]: Displays a list of [count] upcoming songs from the queue ([count] defaults to 6 if not specified).\', Action[ChatMessageEvent, object](OnPeek))\r\n\r\ndef OnUnload():\r\n    ChatManager.RemoveCommand(\'peek\')",
                        Enabled = true,
                        Default = true
                    }
                },
                {
                    new GrooveModule
                    {
                        Name = "queue-random",
                        DisplayName = "Queue Random",
                        Description = "Queues a number of random songs from the collection.",
                        Script = @"from GS.Lib.Events import ChatMessageEvent

def OnQueueRandom(p_Event, p_Data):
    s_SpecialGuest = BroadcastManager.GetGuestForUserID(p_Event.UserID)

    if s_SpecialGuest == None:
        ChatManager.SendChatMessage(""Sorry %s, but you don't have permission to use this feature."" % p_Event.UserName)
        return

    if len(p_Data.strip()) == 0 or not p_Data.strip().isdigit():
        ChatManager.SendChatMessage(""Usage: %squeueRandom [count]"" % SettingsManager.CommandPrefix())
        return

    QueueManager.QueueRandomSongs(int(p_Data.strip()))


ChatManager.RegisterCommand('queueRandom', ' [count]: Adds [count] random songs to the end of the queue ([count] defaults to 1 if not specified).', Action[ChatMessageEvent, object](OnQueueRandom))

def OnUnload():
    ChatManager.RemoveCommand('queueRandom')",
                        Enabled = true,
                        Default = true
                    }
                },
                {
                    new GrooveModule
                    {
                        Name = "add-guests",
                        DisplayName = "Add Guest",
                        Description = "Adds a user as a permanent Special Guest.",
                        Script = @"from GS.Lib.Events import ChatMessageEvent
from GS.Lib.Enums import VIPPermissions

def OnAddGuest(p_Event, p_Data):
    s_SpecialGuest = BroadcastManager.GetGuestForUserID(p_Event.UserID)

    if s_SpecialGuest == None or not s_SpecialGuest.CanAddPermanentGuests:
        ChatManager.SendChatMessage(""Sorry %s, but you don't have permission to use this feature."" % p_Event.UserName)
        return

    s_Parts = p_Data.split()

    if len(s_Parts) != 2:
        ChatManager.SendChatMessage(""Usage: %saddGuest <userID> <userName>"" % SettingsManager.CommandPrefix())
        return

    if not s_Parts[0].strip().isdigit() or len(s_Parts[1].strip()) == 0:
        ChatManager.SendChatMessage(""Usage: %saddGuest <userID> <userName>"" % SettingsManager.CommandPrefix())
        return  

    if not BroadcastManager.AddGuest(s_Parts[1].strip(), int(s_Parts[0]), VIPPermissions.Suggestions | VIPPermissions.ChatModerate):
        ChatManager.SendChatMessage(""The user you specified already has guest permissions."")
        return
      
    ChatManager.SendChatMessage(""Successfully granted user '%s' guest permissions."" % s_Parts[1].strip())

ChatManager.RegisterCommand('addGuest', ' <userid> <userName>: Makes user with user ID <userid> and name <userName> a permanent special guest.', Action[ChatMessageEvent, object](OnAddGuest))

def OnUnload():
    ChatManager.RemoveCommand('addGuest')",
                        Enabled = true,
                        Default = true
                    }
                },
                {
                    new GrooveModule
                    {
                        Name = "remove-guest",
                        DisplayName = "Remove Guest",
                        Description = "Removes special guest permissions from a user.",
                        Script = @"from GS.Lib.Events import ChatMessageEvent
from GS.Lib.Enums import VIPPermissions

def OnRemoveGuest(p_Event, p_Data):
    s_SpecialGuest = BroadcastManager.GetGuestForUserID(p_Event.UserID)

    if s_SpecialGuest == None or not s_SpecialGuest.CanAddPermanentGuests:
        ChatManager.SendChatMessage(""Sorry %s, but you don't have permission to use this feature."" % p_Event.UserName)
        return

    if len(p_Data.strip()) == 0 or not p_Data.strip().isdigit():
        ChatManager.SendChatMessage(""Usage: %sremoveGuest <userID>"" % SettingsManager.CommandPrefix())
        return  

    if not BroadcastManager.RemoveGuest(int(p_Data.strip())):
        ChatManager.SendChatMessage(""The user you specified does not have guest permissions."")
        return
      
    ChatManager.SendChatMessage(""Successfully revoked guest permissions from user with ID '%d'."" % int(p_Data.strip()))

ChatManager.RegisterCommand('removeGuest', ' <userid>: Permanently removes special guest permissions from user with user ID <userid>.', Action[ChatMessageEvent, object](OnRemoveGuest))

def OnUnload():
    ChatManager.RemoveCommand('removeGuest')",
                        Enabled = true,
                        Default = true
                    }
                },
                {
                    new GrooveModule
                    {
                        Name = "unguest",
                        DisplayName = "Unguest",
                        Description = "Unguests one or all users in a broadcast.",
                        Script = @"from GS.Lib.Events import ChatMessageEvent
from GS.Lib.Enums import VIPPermissions

def OnUnguest(p_Event, p_Data):
    s_SpecialGuest = BroadcastManager.GetGuestForUserID(p_Event.UserID)

    if s_SpecialGuest == None or not s_SpecialGuest.CanAddTemporaryGuests:
        ChatManager.SendChatMessage(""Sorry %s, but you don't have permission to use this feature."" % p_Event.UserName)
        return

    if len(p_Data.strip()) == 0:
        BroadcastManager.UnguestAll()
        return

    if not p_Data.strip().isdigit():
        ChatManager.SendChatMessage(""Usage: %sunguest [userID]"" % SettingsManager.CommandPrefix())
        return  

    if not BroadcastManager.HasActiveGuest(int(p_Data)):
        return
      
    BroadcastManager.Unguest(int(p_Data))

ChatManager.RegisterCommand('unguest', ' [userid]: Temporarily removes special guest permissions from user with user ID [userid]. Unguests everyone if [userid] is not specified.', Action[ChatMessageEvent, object](OnUnguest))

def OnUnload():
    ChatManager.RemoveCommand('unguest')",
                        Enabled = true,
                        Default = true
                    }
                },
                {
                    new GrooveModule
                    {
                        Name = "set-title",
                        DisplayName = "Set Title",
                        Description = "Sets the title of the broadcast.",
                        Script = @"from GS.Lib.Events import ChatMessageEvent
from GS.Lib.Enums import VIPPermissions

def OnSetTitle(p_Event, p_Data):
    s_SpecialGuest = BroadcastManager.GetGuestForUserID(p_Event.UserID)

    if s_SpecialGuest == None or not s_SpecialGuest.CanEditTitle:
        ChatManager.SendChatMessage(""Sorry %s, but you don't have permission to use this feature."" % p_Event.UserName)
        return

    if len(p_Data.strip()) < 3:
        ChatManager.SendChatMessage(""Usage: %ssetTitle <title>"" % SettingsManager.CommandPrefix())
        return 
      
    BroadcastManager.SetTitle(p_Data.strip())

ChatManager.RegisterCommand('setTitle', ' <title>: Sets the title of the broadcast.', Action[ChatMessageEvent, object](OnSetTitle))

def OnUnload():
    ChatManager.RemoveCommand('setTitle')",
                        Enabled = true,
                        Default = true
                    }
                },
                {
                    new GrooveModule
                    {
                        Name = "set-description",
                        DisplayName = "Set Description",
                        Description = "Sets the description of the broadcast.",
                        Script = @"from GS.Lib.Events import ChatMessageEvent
from GS.Lib.Enums import VIPPermissions

def OnSetDescription(p_Event, p_Data):
    s_SpecialGuest = BroadcastManager.GetGuestForUserID(p_Event.UserID)

    if s_SpecialGuest == None or not s_SpecialGuest.CanEditDescription:
        ChatManager.SendChatMessage(""Sorry %s, but you don't have permission to use this feature."" % p_Event.UserName)
        return

    if len(p_Data.strip()) < 3:
        ChatManager.SendChatMessage(""Usage: %ssetDescription <title>"" % SettingsManager.CommandPrefix())
        return 
      
    BroadcastManager.SetDescription(p_Data.strip())

ChatManager.RegisterCommand('setDescription', ' <title>: Sets the description of the broadcast.', Action[ChatMessageEvent, object](OnSetDescription))

def OnUnload():
    ChatManager.RemoveCommand('setDescription')",
                        Enabled = true,
                        Default = true
                    }
                },
                {
                    new GrooveModule
                    {
                        Name = "add-to collection",
                        DisplayName = "Add to Collection",
                        Description = "Adds the currently playing song to the song collection.",
                        Script = @"from GS.Lib.Events import ChatMessageEvent

def OnAddToCollection(p_Event, p_Data):
    s_SpecialGuest = BroadcastManager.GetGuestForUserID(p_Event.UserID)

    if s_SpecialGuest == None or not s_SpecialGuest.SuperGuest:
        ChatManager.SendChatMessage(""Sorry %s, but you don't have permission to use this feature."" % p_Event.UserName)
        return

    if not QueueManager.AddPlayingSongToCollection():
        ChatManager.SendChatMessage(""Song already exists in the collection."")
        return

    ChatManager.SendChatMessage(""Song has been successfully added to the collection."")

ChatManager.RegisterCommand('addToCollection', ': Adds the currently playing song to the song collection.', Action[ChatMessageEvent, object](OnAddToCollection))

def OnUnload():
    ChatManager.RemoveCommand('addToCollection')",
                        Enabled = true,
                        Default = true
                    }
                },
                {
                    new GrooveModule
                    {
                        Name = "remove-from collection",
                        DisplayName = "Remove from Collection",
                        Description = "Removes the currently playing song from the collection.",
                        Script = @"from GS.Lib.Events import ChatMessageEvent

def OnRemoveFromCollection(p_Event, p_Data):
    s_SpecialGuest = BroadcastManager.GetGuestForUserID(p_Event.UserID)

    if s_SpecialGuest == None or not s_SpecialGuest.SuperGuest:
        ChatManager.SendChatMessage(""Sorry %s, but you don't have permission to use this feature."" % p_Event.UserName)
        return

    if not QueueManager.RemovePlayingSongFromCollection():
        ChatManager.SendChatMessage(""Song does not exist in the collection."")
        return

    ChatManager.SendChatMessage(""Song has been successfully removed from the collection."")

ChatManager.RegisterCommand('removeFromCollection', ': Removes the currently playing song from the collection.', Action[ChatMessageEvent, object](OnRemoveFromCollection))

def OnUnload():
    ChatManager.RemoveCommand('removeFromCollection')",
                        Enabled = true,
                        Default = true
                    }
                },
                {
                    new GrooveModule
                    {
                        Name = "seek",
                        DisplayName = "Seek",
                        Description = "Seeks to the specified second of the currently playing song.",
                        Script = @"from GS.Lib.Events import ChatMessageEvent

def OnSeek(p_Event, p_Data):
    s_SpecialGuest = BroadcastManager.GetGuestForUserID(p_Event.UserID)

    if s_SpecialGuest == None or not s_SpecialGuest.SuperGuest:
        ChatManager.SendChatMessage(""Sorry %s, but you don't have permission to use this feature."" % p_Event.UserName)
        return

    if len(p_Data.strip()) == 0 or not p_Data.strip().isdigit() or int(p_Data.strip()) < 0:
        ChatManager.SendChatMessage(""Usage: %sseek <seconds>"" % SettingsManager.CommandPrefix())
        return

    QueueManager.SeekCurrentSong(float(p_Data.strip()) * 1000.0)


ChatManager.RegisterCommand('seek', ' <second>: Seeks to the <second> second of the currently playing song.', Action[ChatMessageEvent, object](OnSeek))

def OnUnload():
    ChatManager.RemoveCommand('seek')",
                        Enabled = true,
                        Default = true
                    }
                },
            };

            p_Connection.SaveAll(s_Modules);
        }

    }
}
