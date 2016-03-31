namespace StorybrewCommon.Storyboarding.CommandValues
{
    public interface CommandValue
    {
        float DistanceFrom(object obj);
        string ToOsbString(ExportSettings exportSettings);
    }
}
