namespace StorybrewCommon.Storyboarding
{
    public class OsbSample : StoryboardObject
    {
        private string audioPath = "";
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

        public override void WriteOsb(TextWriter writer, ExportSettings exportSettings, OsbLayer layer, StoryboardTransform transform)
            => writer.WriteLine($"Sample,{((int)Time).ToString(exportSettings.NumberFormat)},{layer},\"{AudioPath.Trim()}\",{((int)Volume).ToString(exportSettings.NumberFormat)}");
    }
}
