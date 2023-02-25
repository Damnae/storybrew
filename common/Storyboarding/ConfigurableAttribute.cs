using System;

namespace StorybrewCommon.Storyboarding
{
    ///<summary> Configurable attribute for variables. </summary>
    [AttributeUsage(AttributeTargets.Field)] public class ConfigurableAttribute : Attribute
    {
        ///<summary> Name of the configurable object, displayed in effect configuration list. </summary>
        public string DisplayName { get; set; }
    }
}