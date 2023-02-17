using System;

namespace StorybrewCommon.Storyboarding
{
    [AttributeUsage(AttributeTargets.Field)]
    public class GroupAttribute : Attribute
    {
        public string Name { get; set; }

        public GroupAttribute(string name)
        {
            Name = name;
        }
    }
}
