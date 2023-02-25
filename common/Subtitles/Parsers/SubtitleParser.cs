using System.IO;

namespace StorybrewCommon.Subtitles.Parsers
{
    internal interface SubtitleParser
    {
        ///<summary> Parses a given subtitle file and returns the parsed set of subtitles. </summary>
        SubtitleSet Parse(string path);

        ///<summary> Parses a given stream that refers to a subtitle file and returns the parsed set of subtitles. </summary>
        SubtitleSet Parse(Stream stream);
    }
}