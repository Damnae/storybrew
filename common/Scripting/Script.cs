using System;

namespace StorybrewCommon.Scripting
{
    /// <summary>
    /// Base class for all scripts
    /// </summary>
    public abstract class Script
    {
        private string identifier;
        public string Identifier
        {
            get { return identifier; }
            set
            {
                if (identifier != null) throw new InvalidOperationException("This script already has an identifier");
                identifier = value;
            }
        }
    }
}
