namespace StorybrewCommon.Storyboarding.CommandValues
{
#pragma warning disable CS1591
    public interface CommandValue
    {
        float DistanceFrom(object obj);
        string ToOsbString(ExportSettings exportSettings);
    }
}