﻿using System;
using GrooveCaster.Models;
using GS.Lib.Enums;
using GS.Lib.Events;
using ServiceStack.OrmLite;

namespace GrooveCaster.Managers
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
            
            Application.Library.RegisterEventHandler(ClientEvent.Authenticated, OnAuthenticated);
            Application.Library.RegisterEventHandler(ClientEvent.AuthenticationFailed, OnAuthenticationFailed);
        }

        internal static void Authenticate()
        {
            if (Authenticating)
                return;

            Authenticating = true;

            if (Application.Library.User.Data != null && Application.Library.User.Data.UserID > 0)
            {
                Application.Library.Chat.Connect(true);
                return;
            }

            using (var s_Db = Database.GetConnection())
            {
                var s_SessionSetting = s_Db.SingleById<CoreSetting>("gssess");
                AuthenticateUsingSession(s_SessionSetting.Value);
                return;
            }
        }

        private static void AuthenticateUsingSession(String p_SessionID)
        {
            if ((AuthenticationResult = Application.Library.User.Authenticate(p_SessionID)) != AuthenticationResult.Success)
            {
                // Session-based authentication failed; retry with stored username and password.
                using (var s_Db = Database.GetConnection())
                {
                    var s_UsernameSetting = s_Db.SingleById<CoreSetting>("gsun");
                    var s_PasswordSetting = s_Db.SingleById<CoreSetting>("gspw");

                    AuthenticateUsingCredentials(s_UsernameSetting.Value, s_PasswordSetting.Value);
                }

                return;
            }

            Application.Library.Chat.Connect(true);
        }

        private static void AuthenticateUsingCredentials(String p_Username, String p_Password)
        {
            if ((AuthenticationResult = Application.Library.User.Authenticate(p_Username, p_Password)) != AuthenticationResult.Success)
            {
                Authenticating = false;
                return;
            }

            // Store new session ID in database.
            using (var s_Db = Database.GetConnection())
                s_Db.Update(new CoreSetting() { Key = "gssess", Value = Application.Library.User.SessionID });

            Application.Library.Chat.Connect(true);
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
    }
}
