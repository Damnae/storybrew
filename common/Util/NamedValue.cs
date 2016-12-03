using System;

namespace StorybrewCommon.Util
{
    [Serializable]
    public struct NamedValue
    {
        public string Name;
        public object Value;

        public override string ToString() => Name;
    }
}
