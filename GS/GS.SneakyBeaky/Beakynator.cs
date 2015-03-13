using System;
using System.Net;
using System.Text.RegularExpressions;

namespace GS.SneakyBeaky
{
    public static class Beakynator
    {
        public static String FetchSecretKey()
        {
            var s_Script = DownloadScript();

            if (s_Script == null)
            {
                return null;
            }

            String s_Key = null;

            var s_Match = Regex.Match(s_Script,
              @"hex_sha1\(\[[a-zA-Z0-9_\.]+,[a-zA-Z0-9_\.]+,([a-zA-Z0-9_]+),[a-zA-Z0-9_\.]+\]\.join\("":""\)",
              RegexOptions.IgnoreCase);

            if (!s_Match.Success || String.IsNullOrWhiteSpace(s_Match.Groups[1].Value))
                return null;

            var s_KeyVar = s_Match.Groups[1].Value;

            var s_StartIndex = s_Script.IndexOf(s_Match.Groups[0].Value, StringComparison.Ordinal);

            if (s_StartIndex == -1)
                return null;

            var s_KeyStartIndex = s_Script.LastIndexOf(s_KeyVar + "=\"", s_StartIndex, StringComparison.Ordinal);

            if (s_KeyStartIndex == -1)
                return null;

            s_KeyStartIndex += s_KeyVar.Length + 2;

            var s_KeyEndIndex = s_Script.IndexOf("\"", s_KeyStartIndex, StringComparison.Ordinal);

            if (s_KeyEndIndex == -1)
                return null;

            return s_Script.Substring(s_KeyStartIndex, s_KeyEndIndex - s_KeyStartIndex);
        }

        private static String DownloadScript()
        {
            var s_ScriptURL = GetScriptURL();

            if (String.IsNullOrWhiteSpace(s_ScriptURL))
                return null;
            try
            {
                using (var s_Client = new WebClient())
                    return s_Client.DownloadString(s_ScriptURL);
            }
            catch (Exception)
            {
                // TODO: More detailed information.
                Console.WriteLine("Failed to download GS APP Script; are we banned?");
                return null;
            }
        }

        private static String GetScriptURL()
        {
            String s_HTMLContents;

            try
            {
                using (var s_Client = new WebClient())
                    s_HTMLContents = s_Client.DownloadString("http://grooveshark.com");
            }
            catch (Exception)
            {
                // TODO: More detailed information.
                Console.WriteLine("Failed to download GS page; are we banned?");
                return null;
            }

            // Find the app script href.
            var s_Match = Regex.Match(s_HTMLContents,
                @"<link rel=""subresource"" href=""(https?://[a-zA-Z0-9\.\-_/]+/app_[0-9]+.js)"" />",
                RegexOptions.IgnoreCase);

            if (s_Match.Success && !String.IsNullOrWhiteSpace(s_Match.Groups[1].Value)) 
                return s_Match.Groups[1].Value;

            Console.WriteLine("Failed to locate app script link.");
            return null;
        }
    }
}
