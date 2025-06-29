namespace StorybrewCommon.Storyboarding.Commands
{
    public interface ICommand : IComparable<ICommand>
    {
        double StartTime { get; }
        double EndTime { get; }
        int Cost { get; }

        bool IsFragmentableAt(double time);
        void WriteOsb(TextWriter writer, ExportSettings exportSettings, StoryboardTransform transform, int indentation);
    }
}