using System;
using System.Globalization;

namespace StorybrewCommon.Mapset
{
    [Serializable]
    public class ControlPoint
    {
        public int Offset;
        public double BeatDuration;
        public int BeatPerMeasure;
        public int SampleType;
        public int SampleSet;
        public float Volume;
        public bool IsInherited;
        public bool IsKiai;

        public double Bpm => BeatDuration == 0 ? 0 : 60000 / BeatDuration;
        public double SliderMultiplier => BeatDuration > 0 ? 1.0 : -(BeatDuration / 100.0);

        public override string ToString() => IsInherited ? $"{Offset}ms, {SliderMultiplier}x, {BeatPerMeasure}/4" : $"{Offset}ms, {Bpm}bpm, {BeatPerMeasure}/4";

        public static ControlPoint Parse(string line)
        {
            var values = line.Split(',');
            if (values.Length < 2) throw new InvalidOperationException($"Control point has less than the 2 required parameters: {line}");

            return new ControlPoint()
            {
                Offset = int.Parse(values[0]),
                BeatDuration = double.Parse(values[1], CultureInfo.InvariantCulture),
                BeatPerMeasure = values.Length > 2 ? int.Parse(values[2]) : 4,
                SampleType = values.Length > 3 ? int.Parse(values[3]) : 1,
                SampleSet = values.Length > 4 ? int.Parse(values[4]) : 1,
                Volume = values.Length > 5 ? int.Parse(values[5]) : 100,
                IsInherited = values.Length > 6 ? int.Parse(values[6]) == 0 : false,
                IsKiai = values.Length > 7 ? int.Parse(values[7]) != 0 : false,
            };
        }
    }
}
