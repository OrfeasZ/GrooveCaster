using System;
using GrooveCasterServer.Models;
using GS.Lib.Enums;
using GS.Lib.Events;
using ServiceStack.OrmLite;

namespace GrooveCasterServer.Managers
{
    public static class UserManager
    {
        public static bool Authenticating { get; set; }
        public static AuthenticationResult AuthenticationResult { get; set; }

        static UserManager()
        {
            Authenticating = false;
        }

        public static void Init()
        {
            Authenticating = false;
            AuthenticationResult = AuthenticationResult.Success;
            
            Program.Library.RegisterEventHandler(ClientEvent.Authenticated, OnAuthenticated);
            Program.Library.RegisterEventHandler(ClientEvent.AuthenticationFailed, OnAuthenticationFailed);
        }

        public static void Authenticate()
        {
            if (Authenticating)
                return;

            Authenticating = true;

            if (Program.Library.User.Data != null && Program.Library.User.Data.UserID > 0)
            {
                Program.Library.Chat.Connect();
                return;
            }

            using (var s_Db = Program.DbConnectionString.OpenDbConnection())
            {
                var s_SessionSetting = s_Db.SingleById<CoreSetting>("gssess");
                AuthenticateUsingSession(s_SessionSetting.Value);
                return;
            }
        }

        private static void AuthenticateUsingSession(String p_SessionID)
        {
            if ((AuthenticationResult = Program.Library.User.Authenticate(p_SessionID)) != AuthenticationResult.Success)
            {
                // Session-based authentication failed; retry with stored username and password.
                using (var s_Db = Program.DbConnectionString.OpenDbConnection())
                {
                    var s_UsernameSetting = s_Db.SingleById<CoreSetting>("gsun");
                    var s_PasswordSetting = s_Db.SingleById<CoreSetting>("gspw");

                    AuthenticateUsingCredentials(s_UsernameSetting.Value, s_PasswordSetting.Value);
                }

                return;
            }

            Program.Library.Chat.Connect();
        }

        private static void AuthenticateUsingCredentials(String p_Username, String p_Password)
        {
            if ((AuthenticationResult = Program.Library.User.Authenticate(p_Username, p_Password)) != AuthenticationResult.Success)
            {
                Authenticating = false;
                return;
            }

            // Store new session ID in database.
            using (var s_Db = Program.DbConnectionString.OpenDbConnection())
                s_Db.Update(new CoreSetting() { Key = "gssess", Value = Program.Library.User.SessionID });

            Program.Library.Chat.Connect();
        }

        private static void OnAuthenticated(SharkEvent p_SharkEvent)
        {
            Authenticating = false;
            AuthenticationResult = AuthenticationResult.Success;

            QueueManager.FetchCollectionSongs();

            if (QueueManager.CollectionSongs.Count < 2)
                return;

            BroadcastManager.CreateBroadcast();
        }

        private static void OnAuthenticationFailed(SharkEvent p_SharkEvent)
        {
            Authenticating = false;
            AuthenticationResult = AuthenticationResult.InternalError;
        }

        public static void GuestUser(Int64 p_UserID, String p_UserName)
        {
            if (Program.Library.Broadcast.SpecialGuests.Contains(p_UserID))
            {
                Program.Library.Broadcast.RemoveSpecialGuest(p_UserID);
                return;
            }

            var s_SpecialGuest = GetGuestForUserID(p_UserID);

            if (s_SpecialGuest == null)
            {
                Program.Library.Chat.SendChatMessage("Sorry " + p_UserName + ", but you don't have permissions to use this feature.");
                return;
            }

            Program.Library.Broadcast.AddSpecialGuest(s_SpecialGuest.UserID, s_SpecialGuest.Permissions);
        }

        public static void AddGuest(Int64 p_UserID, String p_Name)
        {
            
        }

        public static void RemoveGuest(Int64 p_UserID)
        {
            
        }

        public static void UnguestAll()
        {
            foreach (var s_UserID in Program.Library.Broadcast.SpecialGuests)
            {
                Program.Library.Broadcast.RemoveSpecialGuest(s_UserID);
                return;
            }
        }

        public static SpecialGuest GetGuestForUserID(Int64 p_UserID)
        {
            using (var s_Db = Program.DbConnectionString.OpenDbConnection())
                return s_Db.SingleById<SpecialGuest>(p_UserID);
        }
    }
}
