using System;
using System.Collections.Generic;
using GrooveCaster.Models;
using IronPython.Hosting;
using Microsoft.Scripting;
using Microsoft.Scripting.Hosting;
using ServiceStack.OrmLite;

namespace GrooveCaster.Managers
{
    public static class ModuleManager
    {
        private static Dictionary<string, ModuleScript> m_LoadedModules;
        private static ScriptEngine m_ScriptEngine;

        internal static Dictionary<string, string> LoadExceptions { get; set; } 

        static ModuleManager()
        {
            
        }

        internal static void Init()
        {
            LoadExceptions = new Dictionary<string, string>();
            m_LoadedModules = new Dictionary<string, ModuleScript>();

            m_ScriptEngine = Python.CreateEngine();

            ReloadModules();
        }

        internal static void ReloadModules()
        {
            foreach (var s_Pair in m_LoadedModules)
            {
                try
                {
                    Action s_Function;

                    if (!s_Pair.Value.Scope.TryGetVariable("OnUnload", out s_Function))
                        continue;

                    if (s_Function != null)
                        s_Function();
                }
                catch
                {
                    continue;
                }
            }

            m_ScriptEngine.Runtime.Shutdown();

            m_ScriptEngine = Python.CreateEngine();
            
            LoadExceptions.Clear();
            m_LoadedModules.Clear();

            foreach (var s_Module in GetModules())
                if (s_Module.Enabled)
                    CompileModule(s_Module);
        }

        internal static bool OnFetchingNextSong()
        {
            foreach (var s_Pair in m_LoadedModules)
            {
                try
                {
                    Func<bool> s_Function;
                    if (!s_Pair.Value.Scope.TryGetVariable("OnFetchingNextSong", out s_Function))
                        continue;

                    if (s_Function != null && !s_Function())
                        return false;
                }
                catch
                {
                    continue;
                }
            }

            return true;
        }

        internal static void CompileModule(GrooveModule p_Module)
        {
            if (m_LoadedModules.ContainsKey(p_Module.Name) || !p_Module.Enabled)
                return;

            try
            {
                var s_Scope = m_ScriptEngine.CreateScope();

                s_Scope.ImportModule("clr");

                m_ScriptEngine.Execute("import clr", s_Scope);
                m_ScriptEngine.Execute("from clr import Convert", s_Scope);

                m_ScriptEngine.Execute("clr.AddReference('System.Core')", s_Scope);
                m_ScriptEngine.Execute("from System import Action", s_Scope);
                m_ScriptEngine.Execute("from System.Collections.Generic import List", s_Scope);

                m_ScriptEngine.Execute("clr.AddReference('GS.Lib')", s_Scope);
                m_ScriptEngine.Execute("from GS.Lib import *", s_Scope);

                m_ScriptEngine.Execute("clr.AddReference('GrooveCasterServer')", s_Scope);
                m_ScriptEngine.Execute("from GrooveCaster import *", s_Scope);
                m_ScriptEngine.Execute("from GrooveCaster.Managers import BroadcastManager, ChatManager, QueueManager, SettingsManager, UserManager, SuggestionManager, PlaylistManager", s_Scope);
                m_ScriptEngine.Execute("from GrooveCaster.Application import Library as SharpShark", s_Scope);
                m_ScriptEngine.Execute("from GrooveCaster.Util import ModuleTimer as Timer", s_Scope);


                var s_ScriptSource = m_ScriptEngine.CreateScriptSourceFromString(p_Module.Script.Trim(), SourceCodeKind.File);
                var s_Script = s_ScriptSource.Execute(s_Scope);

                m_LoadedModules.Add(p_Module.Name, new ModuleScript()
                {
                    Source = s_ScriptSource,
                    Script = s_Script,
                    Scope = s_Scope
                });
            }
            catch (Exception s_Exception)
            {
                Console.WriteLine(s_Exception);
                LoadExceptions.Add(p_Module.Name, s_Exception.Message);
            }
        }

        internal static IEnumerable<GrooveModule> GetModules()
        {
            using (var s_Db = Database.GetConnection())
                return s_Db.Select<GrooveModule>();
        }

        internal static GrooveModule GetModule(String p_Name)
        {
            using (var s_Db = Database.GetConnection())
                return s_Db.SingleById<GrooveModule>(p_Name.Trim().ToLowerInvariant());
        }

        internal static void DisableModule(GrooveModule p_Module)
        {
            p_Module.Enabled = false;
            UpdateModule(p_Module);
        }

        internal static void EnableModule(GrooveModule p_Module)
        {
            p_Module.Enabled = true;
            UpdateModule(p_Module);
        }

        internal static void UpdateModule(GrooveModule p_Module)
        {
            using (var s_Db = Database.GetConnection())
                s_Db.Update(p_Module);
        }

        internal static void RemoveModule(GrooveModule p_Module)
        {
            if (p_Module.Default)
                return;

            using (var s_Db = Database.GetConnection())
                s_Db.Delete(p_Module);
        }
    }
}
