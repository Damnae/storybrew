using System.Collections.Generic;

namespace StorybrewCommon.Subtitles
{
#pragma warning disable CS1591
    public class SubtitleSet
    {
        readonly List<SubtitleLine> lines = new List<SubtitleLine>();
        public IEnumerable<SubtitleLine> Lines => lines;

        public SubtitleSet(IEnumerable<SubtitleLine> lines) => this.lines = new List<SubtitleLine>(lines);
    }
}