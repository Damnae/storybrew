namespace StorybrewCommon.Subtitles
{
    public class SubtitleSet
    {
        private readonly List<SubtitleLine> lines = new List<SubtitleLine>();
        public IEnumerable<SubtitleLine> Lines => lines;

        public SubtitleSet(IEnumerable<SubtitleLine> lines)
        {
            this.lines = new List<SubtitleLine>(lines);
        }
    }
}
