using System;
using StorybrewCommon.Scripting;

namespace StorybrewEditor.Scripting
{
    public interface ScriptContainer<TScript> : IDisposable
        where TScript : Script
    {
        string Name { get; }
        string ScriptTypeName { get; }
        string SourcePath { get; }
        bool HasScript { get; }

        event EventHandler OnScriptChanged;

        TScript CreateScript();
        void ReloadScript();
    }
}