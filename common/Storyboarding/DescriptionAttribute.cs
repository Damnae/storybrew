using System;

namespace StorybrewCommon.Storyboarding
{
    ///<summary> Represents a description attribute that can be displayed on configurable variables. </summary>
    [AttributeUsage(AttributeTargets.Field)] public class DescriptionAttribute : Attribute
    {
        ///<summary> Represents the content of the description attribute. </summary>
        public string Content { get; set; }

        ///<summary> Constructs a new description attribute that applies to a variable with given description/content. </summary>
        public DescriptionAttribute(string content) => Content = content;
    }
}