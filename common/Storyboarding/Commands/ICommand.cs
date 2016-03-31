
namespace StorybrewCommon.Storyboarding.Commands
{
    public interface ICommand
    {
        double StartTime { get; }
        double EndTime { get; }
        bool Enabled { get; }

        string ToOsbString(ExportSettings exportSettings);
    }
}