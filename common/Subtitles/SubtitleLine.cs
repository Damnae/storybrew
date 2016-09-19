using System;

namespace StorybrewCommon.Subtitles
{
    public class SubtitleLine
    {
        private double startTime;
        public double StartTime => startTime;

        private double endTime;
        public double EndTime => endTime;

        private string text;
        public string Text => text;

        public SubtitleLine(double startTime, double endTime, string text)
        {
            this.startTime = startTime;
            this.endTime = endTime;
            this.text = text;
        }
    }
}
