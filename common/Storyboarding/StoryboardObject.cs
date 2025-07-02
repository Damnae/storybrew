namespace StorybrewCommon.Storyboarding
{
    public abstract class StoryboardObject
    {
        public abstract double StartTime { get; }
        public abstract double EndTime { get; }

        public abstract void WriteOsb(TextWriter writer, ExportSettings exportSettings, OsbLayer layer, StoryboardTransform transform, CancellationToken token = default);
    }
}
