using System.IO;

namespace StorybrewCommon.Storyboarding
{
    ///<summary> A type of <see cref="StoryboardObject"/> that plays an audio file. </summary>
    public class OsbSample : StoryboardObject
    {
        string audioPath = "";

        ///<summary> Gets the audio path of this audio sample. </summary>
        public string AudioPath
        {
            get => audioPath;
            set
            {
                if (audioPath == value) return;
                new FileInfo(value);
                audioPath = value;
            }
        }

        ///<summary> The time of which this audio is played. </summary>
        public double Time;

        ///<summary> The volume (out of 100) of this audio sample. </summary>
        public double Volume = 100;

        ///<inheritdoc/>
        public override double StartTime => Time;

        ///<inheritdoc/>
        public override double EndTime => Time;

        ///<summary/>
        public override void WriteOsb(TextWriter writer, ExportSettings exportSettings, OsbLayer layer) => writer.WriteLine(
        $"Sample,{((int)Time).ToString(exportSettings.NumberFormat)},{layer},\"{AudioPath.Trim()}\",{((int)Volume).ToString(exportSettings.NumberFormat)}");
    }
}