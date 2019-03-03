namespace StorybrewCommon.Subtitles
{
    public class SubtitleLine
    {
        public double StartTime { get; }
        public double EndTime { get; }
        public string Text { get; }

        public SubtitleLine(double startTime, double endTime, string text)
        {
            StartTime = startTime;
            EndTime = endTime;
            Text = text;
        }
    }
}
