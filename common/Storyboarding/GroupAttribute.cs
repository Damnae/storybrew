using System;

namespace StorybrewCommon.Storyboarding
{
    ///<summary> Represents a group for configurable variables. </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class GroupAttribute : Attribute
    {
        ///<summary> The name of the group. </summary>
        public string Name { get; set; }

        ///<summary/>
        public GroupAttribute(string name) => Name = name;
    }
}