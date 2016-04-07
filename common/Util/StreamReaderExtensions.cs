using System;
using System.IO;

namespace StorybrewCommon.Util
{
    public static class StreamReaderExtensions
    {
        /// <summary>
        /// Calls the action with key and value, until it finds a blank line or the end of the file.
        /// </summary>
        public static void ParseKeyValueSection(this StreamReader reader, Action<string, string> action)
        {
            string line;
            while ((line = reader.ReadLine()) != null)
            {
                line = line.Trim();
                if (line.Length == 0) break;

                string key, value;
                parseKeyValue(line, out key, out value);
                action(key, value);
            }
        }

        private static void parseKeyValue(string line, out string key, out string value)
        {
            var separatorIndex = line.IndexOf(":");
            if (separatorIndex == -1) throw new InvalidDataException($"{line} is not a key/value");

            key = line.Substring(0, separatorIndex).Trim();
            value = line.Substring(separatorIndex + 1, line.Length - 1 - separatorIndex).Trim();
        }
    }
}
