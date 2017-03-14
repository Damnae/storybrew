using System.IO;

namespace StorybrewCommon.Storyboarding
{
    public class OsbSample : StoryboardObject
    {
        public string audioPath = "";
        public string AudioPath
        {
            get { return audioPath; }
            set
            {
                if (audioPath == value) return;
                new FileInfo(value);
                audioPath = value;
            }
        }

        public double Time;
        public double Volume = 100;

        public override double StartTime => Time;
        public override double EndTime => Time;

        public override void WriteOsb(TextWriter writer, ExportSettings exportSettings, OsbLayer layer)
            => writer.WriteLine($"Sample,{((int)Time).ToString(exportSettings.NumberFormat)},{layer},\"{AudioPath}\",{((int)Volume).ToString(exportSettings.NumberFormat)}");
    }
}
