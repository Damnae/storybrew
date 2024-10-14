﻿using System.Text;

namespace StorybrewCommon.Subtitles.Parsers
{
    public class SrtParser
    {
        public SubtitleSet Parse(string path)
        {
            using (var stream = BrewLib.Util.Misc.WithRetries(() => new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read)))
                return Parse(stream);
        }

        public SubtitleSet Parse(Stream stream)
        {
            var lines = new List<SubtitleLine>();
            foreach (var block in parseBlocks(stream))
            {
                var blockLines = block.Split('\n');
                var timestamps = blockLines[1].Split(new string[] { "-->" }, StringSplitOptions.None);
                var startTime = parseTimestamp(timestamps[0]);
                var endTime = parseTimestamp(timestamps[1]);
                var text = string.Join("\n", blockLines, 2, blockLines.Length - 2);
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
                    else sb.AppendLine(line);
                }

                var endBlock = sb.ToString().Trim();
                if (endBlock.Length > 0) yield return endBlock;
            }
        }

        private double parseTimestamp(string timestamp)
            => TimeSpan.Parse(timestamp.Replace(',', '.')).TotalMilliseconds;
    }
}
