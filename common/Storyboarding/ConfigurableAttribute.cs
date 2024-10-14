namespace StorybrewCommon.Storyboarding
{
    [AttributeUsage(AttributeTargets.Field)]
    public class ConfigurableAttribute : Attribute
    {
        public string DisplayName { get; set; }
    }
}
