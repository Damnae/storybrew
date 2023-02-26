using System;

namespace StorybrewCommon.Storyboarding
{
    ///<summary> Configurable attribute for variables. </summary>
    [AttributeUsage(AttributeTargets.Field)] public class ConfigurableAttribute : Attribute
    {
        ///<summary> Name of the configurable object, displayed in effect configuration list. </summary>
        public string DisplayName { get; set; }
    }

    ///<summary> Represents a description attribute that can be displayed on configurable variables. </summary>
    [AttributeUsage(AttributeTargets.Field)] public class DescriptionAttribute : Attribute
    {
        ///<summary> Represents the content of the description attribute. </summary>
        public string Content { get; set; }

        ///<summary> Constructs a new description attribute that applies to a variable with given description/content. </summary>
        public DescriptionAttribute(string content) => Content = content;
    }

    ///<summary> Represents a grouping mechanism for configurable variables. </summary>
    [AttributeUsage(AttributeTargets.Field)] public class GroupAttribute : Attribute
    {
        ///<summary> The name of the group. </summary>
        public string Name { get; set; }

        ///<summary> Creates a new group of configurable variables (below this attribute) with given display name. </summary>
        public GroupAttribute(string name) => Name = name;
    }
}