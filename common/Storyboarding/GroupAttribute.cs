using System;

namespace StorybrewCommon.Storyboarding
{
#pragma warning disable CS1591
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