using System;

namespace StorybrewCommon.Storyboarding
{
    ///<summary> Represents a description attribute that can be displayed on configurable variables. </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class DescriptionAttribute : Attribute
    {
        ///<summary> Represents the content of the description attribute. </summary>
        public string Content { get; set; }

        ///<summary/>
        public DescriptionAttribute(string content) => Content = content;
    }
}