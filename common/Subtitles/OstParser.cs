using StorybrewCommon.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace StorybrewCommon.Subtitles
{
    // Parser for Notepadediting with osu!timestamps
    public class OstParser
    {
        public SubtitleSet Parse(string path)
        {
            using (var stream = Misc.WithRetries(() => new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read)))
                return Parse(stream);
        }


        public SubtitleSet Parse(Stream stream)
        {
            var lines = new List<SubtitleLine>();
            foreach (var block in parseBlocks(stream))
            {
                var blockLines = block.Split('\n');
                List<String> timestamps = (blockLines[0].Contains("-") && blockLines[0].IndexOf('-') != blockLines[0].Trim().Length - 1) ? blockLines[0].Split('-').ToList() : blockLines[0].Split(' ').ToList();
                timestamps.RemoveAll(String.IsNullOrEmpty);
                var startTime = parseTimestamp(timestamps[0]);
                var endTime = parseTimestamp(timestamps[1]);
                var text = string.Join("\n", blockLines, 1, blockLines.Length - 1);
                lines.Add(new SubtitleLine(startTime, endTime, text));
            }
            return new SubtitleSet(lines);
        }

        private IEnumerable<string> parseBlocks(Stream stream)
        {
            using (var reader = new StreamReader(stream))
            {
                var sb = new StringBuilder();

                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    if (string.IsNullOrEmpty(line.Trim()))
                    {
                        var block = sb.ToString().Trim();
                        if (block.Length > 0) yield return block;
                        sb.Clear();
                    }
                    else
                    {
                        if (line[0] == '_') sb.AppendLine("");
                        else sb.AppendLine(line);                     
                    }
                }

                var endBlock = sb.ToString().Trim();
                if (endBlock.Length > 0) yield return endBlock;
            }
        }


        private double parseTimestamp(string timestamp)
        {
            if (timestamp.Contains(":"))    // compose mode
            {
                if (!timestamp.Contains("."))
                {
                    StringBuilder sb = new StringBuilder(timestamp);
                    sb[timestamp.LastIndexOf(':')] = '.';
                    timestamp = sb.ToString();
                }
                if (timestamp.Contains("(")) timestamp = timestamp.Substring(0, timestamp.IndexOf('('));    
                if (timestamp.Split(':').Length < 3) timestamp = "00:" + timestamp.Trim();      //parser requires hours
                return TimeSpan.Parse(timestamp).TotalMilliseconds;
            }
            return Double.Parse(timestamp); // design mode
        }
            
    }
}
