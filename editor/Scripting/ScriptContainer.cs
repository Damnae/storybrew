using StorybrewCommon.Scripting;
using System;
using System.Collections.Generic;

namespace StorybrewEditor.Scripting
{
    public interface ScriptContainer<TScript> : IDisposable
        where TScript : Script
    {
        string Name { get; }
        string ScriptTypeName { get; }
        string MainSourcePath { get; }
        IEnumerable<string> ReferencedAssemblies { get; set; }
        bool HasScript { get; }

        event EventHandler OnScriptChanged;

        TScript CreateScript();
        void ReloadScript();
    }
}