using System;
using System.Collections.Generic;
using System.IO;

namespace StorybrewCommon.Subtitles
{
    public class SbvParser
    {
        public SubtitleSet Parse(String path)
        {
            string[] rawLines = File.ReadAllLines(path);
            List<SubtitleLine> lines = new List<SubtitleLine>();

            var startTime = 0d;
            var endTime = 0d;
            var text = "";

            foreach (var line in rawLines)
            {
                String[] times = line.Split(',');
                if (times.Length == 2 && !line.Contains(" "))
                {
                    if (times[0].Split(':').Length == 3 && times[1].Split(':').Length == 3) //Make sure that this line is a blockstart!
                    {
                        if (text != "") //Had the previous block content?
                        {
                            lines.Add(new SubtitleLine(startTime, endTime, text));
                            text = "";
                        }

                        startTime = parseTimestamp(times[0]);
                        endTime = parseTimestamp(times[1]);
                        Console.WriteLine(startTime + "");
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
