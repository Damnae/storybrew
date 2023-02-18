using System;

namespace StorybrewEditor.Scripting
{
    public class ScriptLoadingException : Exception
    {
        public ScriptLoadingException() { }
        public ScriptLoadingException(string message) : base(message) { }
        public ScriptLoadingException(string message, Exception innerException) : base(message, innerException) { }
    }
}