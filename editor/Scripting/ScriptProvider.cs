using StorybrewCommon.Scripting;
using System;
using System.Reflection;

namespace StorybrewEditor.Scripting
{
    public class ScriptProvider<TScript> : MarshalByRefObject
        where TScript : Script
    {
        private readonly string identifier = Guid.NewGuid().ToString();
        private Type type;

        public ScriptProvider(Type type)
        {
            this.type = type;
        }

        public TScript CreateScript()
        {
            var script = (TScript)Activator.CreateInstance(type);
            script.Identifier = identifier;
            return script;
        }
    }
}
