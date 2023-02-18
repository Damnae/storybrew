namespace StorybrewCommon.Subtitles
{
    ///<summary> Interpreted lines for subtitle files. </summary>
    public class SubtitleLine
    {
        ///<summary> The start time of the subtitle line. </summary>
        public double StartTime { get; }

        ///<summary> The end time of the subtitle line. </summary>
        public double EndTime { get; }

        ///<summary> The text in the subtitle line. </summary>
        public string Text { get; }

        ///<summary> Constructs a <see cref="SubtitleLine"/>. </summary>
        ///<param name="startTime"> The start time of the subtitle line. </param>
        ///<param name="endTime"> The end time of the subtitle line. </param>
        ///<param name="text"> The text in the subtitle line. </param>
        public SubtitleLine(double startTime, double endTime, string text)
        {
            StartTime = startTime;
            EndTime = endTime;
            Text = text;
        }
    }
}