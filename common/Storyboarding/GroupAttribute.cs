using System;

namespace StorybrewCommon.Storyboarding
{
    ///<summary> Represents a grouping mechanism for configurable variables. </summary>
    [AttributeUsage(AttributeTargets.Field)] public class GroupAttribute : Attribute
    {
        ///<summary> The name of the group. </summary>
        public string Name { get; set; }

        ///<summary> Creates a new group of configurable variables (below this attribute) with given display name. </summary>
        public GroupAttribute(string name) => Name = name;
    }
}