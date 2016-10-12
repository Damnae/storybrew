using System;
using System.IO;

namespace StorybrewCommon.Util
{
    public static class StreamReaderExtensions
    {
        public static void ParseSections(this StreamReader reader, Action<string> action)
        {
            string line;
            while ((line = reader.ReadLine()) != null)
            {
                line = line.Trim();
                if (line.StartsWith("[") && line.EndsWith("]"))
                {
                    var sectionName = line.Substring(1, line.Length - 2);
                    action(sectionName);
                }
            }
        }

        /// <summary>
        /// Calls the action with the content of a line, until it finds a blank line or the end of the file.
        /// </summary>
        public static void ParseSectionLines(this StreamReader reader, Action<string> action)
        {
            string line;
            while ((line = reader.ReadLine()) != null)
            {
                line = line.Trim();
                if (line.Length == 0) return;

                action(line);
            }
        }

        /// <summary>
        /// Calls the action with key and value, until it finds a blank line or the end of the file.
        /// </summary>
        public static void ParseKeyValueSection(this StreamReader reader, Action<string, string> action)
        {
            reader.ParseSectionLines(line =>
            {
                var separatorIndex = line.IndexOf(":");
                if (separatorIndex == -1) throw new InvalidDataException($"{line} is not a key/value");

                var key = line.Substring(0, separatorIndex).Trim();
                var value = line.Substring(separatorIndex + 1, line.Length - 1 - separatorIndex).Trim();

                action(key, value);
            });
        }
    }
}
