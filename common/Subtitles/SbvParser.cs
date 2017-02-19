using System;
using System.Collections.Generic;
using System.IO;

namespace StorybrewCommon.Subtitles
{
    public class SbvParser
    {
        public SubtitleSet Parse(String path)
        {
            var rawLines = File.ReadAllLines(path);
            var lines = new List<SubtitleLine>();

            var startTime = 0d;
            var endTime = 0d;
            var text = "";

            foreach (var line in rawLines)
            {
                var times = line.Split(',');
                if (times.Length == 2 && !line.Contains(" "))
                {
                    //Make sure that this line is a blockstart!
                    if (times[0].Split(':').Length == 3 && times[1].Split(':').Length == 3)
                    {
                        //Had the previous block content?
                        if (text != "")
                        {
                            lines.Add(new SubtitleLine(startTime, endTime, text));
                            text = "";
                        }

                        startTime = parseTimestamp(times[0]);
                        endTime = parseTimestamp(times[1]);
                        continue;
                    }
                }
                text += line;
            }
            if (text != "") lines.Add(new SubtitleLine(startTime, endTime, text));
            return new SubtitleSet(lines);
        }

        private double parseTimestamp(string timestamp)
            => TimeSpan.Parse(timestamp).TotalMilliseconds;
    }
}
