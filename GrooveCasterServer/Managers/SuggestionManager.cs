using System;
using System.Collections.Generic;
using System.Linq;
using GrooveCaster.Models;
using GS.Lib.Enums;
using GS.Lib.Events;
using Microsoft.Scripting.Utils;

namespace GrooveCaster.Managers
{
    public static class SuggestionManager
    {
        public static Dictionary<Int64, SongSuggestion> Suggestions { get; set; } 

        static SuggestionManager()
        {
        }

        internal static void Init()
        {
            Suggestions = new Dictionary<long, SongSuggestion>();

            Program.Library.RegisterEventHandler(ClientEvent.SongSuggestion, OnSongSuggestion);
            Program.Library.RegisterEventHandler(ClientEvent.SongSuggestionRemoved, OnSongSuggestionRemoved);
            Program.Library.RegisterEventHandler(ClientEvent.SongSuggestionRejected, OnSongSuggestionRejected);
        }

        private static void OnSongSuggestion(SharkEvent p_SharkEvent)
        {
            var s_Event = (SongSuggestionEvent) p_SharkEvent;

            lock (Suggestions)
            {
                SongSuggestion s_Suggestion;
             
                // No entry exists for this suggestion yet; create one.
                if (!Suggestions.TryGetValue(s_Event.SongID, out s_Suggestion))
                {
                    s_Suggestion = new SongSuggestion()
                    {
                        SongID = s_Event.SongID,
                        SongName = s_Event.SongName,
                        AlbumID = s_Event.AlbumID,
                        AlbumName = s_Event.AlbumName,
                        ArtistID = s_Event.ArtistID,
                        ArtistName = s_Event.ArtistName,
                        Suggester = new SimpleUser()
                        {
                            UserID = s_Event.UserID,
                            Name = s_Event.User.Username,
                            ProfilePicture = s_Event.User.Picture
                        },
                        OtherSuggesters = new List<SimpleUser>()
                    };

                    Suggestions.Add(s_Event.SongID, s_Suggestion);
                    return;
                }

                // This user has already suggested this song; ignore.
                if (s_Suggestion.Suggester.UserID == s_Event.UserID || 
                    s_Suggestion.OtherSuggesters.Any(p_User => p_User.UserID == s_Event.UserID))
                    return;

                s_Suggestion.OtherSuggesters.Add(new SimpleUser()
                {
                    UserID = s_Event.UserID,
                    Name = s_Event.User.Username,
                    ProfilePicture = s_Event.User.Picture
                });
            }
        }

        private static void OnSongSuggestionRemoved(SharkEvent p_SharkEvent)
        {
            var s_Event = (SongSuggestionRemovalEvent) p_SharkEvent;

            lock (Suggestions)
            {
                SongSuggestion s_Suggestion;

                // This suggestion doesn't even exist.
                if (!Suggestions.TryGetValue(s_Event.SongID, out s_Suggestion))
                    return;

                // This is the last suggestion; completely remove it.
                if (s_Suggestion.Suggester.UserID == s_Event.UserID &&
                    s_Suggestion.OtherSuggesters.Count == 0)
                {
                    Suggestions.Remove(s_Event.SongID);
                    return;
                }

                // This is an upvote removal on a suggestion by another user.
                var s_Index = CollectionUtils.FindIndex(s_Suggestion.OtherSuggesters,
                    p_User => p_User.UserID == s_Event.UserID);

                if (s_Index != -1)
                {
                    s_Suggestion.OtherSuggesters.RemoveAt(s_Index);
                    return;
                }

                // The initial suggester is removing his suggestion. Switch suggesters.
                if (s_Suggestion.Suggester.UserID == s_Event.UserID &&
                    s_Suggestion.OtherSuggesters.Count > 0)
                {
                    s_Suggestion.Suggester = s_Suggestion.OtherSuggesters[0];
                    s_Suggestion.OtherSuggesters.RemoveAt(0);
                }
            }
        }

        private static void OnSongSuggestionRejected(SharkEvent p_SharkEvent)
        {
            var s_Event = (SongSuggestionRejectionEvent) p_SharkEvent;

            lock (Suggestions)
                Suggestions.Remove(s_Event.SongID);
        }
    }
}
