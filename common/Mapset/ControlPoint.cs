using System;
using System.Globalization;

namespace StorybrewCommon.Mapset
{
    [Serializable]
    public class ControlPoint : IComparable<ControlPoint>
    {
        public static readonly ControlPoint Default = new ControlPoint();

        public double Offset;
        public int BeatPerMeasure = 4;
        public int SampleType = 1;
        public SampleSet SampleSet = SampleSet.Normal;
        public float Volume = 100;
        public bool IsInherited;
        public bool IsKiai;
        public bool OmitFirstBarLine;

        private double beatDurationSV = 60000 / 120;
        public double BeatDuration
        {
            get
            {
                if (IsInherited) throw new InvalidOperationException("Control points don't have a beat duration, use timing points");
                return beatDurationSV;
            }
        }
        public double Bpm => BeatDuration == 0 ? 0 : 60000 / BeatDuration;
        public double SliderMultiplier => beatDurationSV > 0 ? 1.0 : -(beatDurationSV / 100.0);

        public int CompareTo(ControlPoint other)
        {
            var value = (int)(Offset - other.Offset);
            return value != 0 ? value : (other.IsInherited ? 0 : 1) - (IsInherited ? 0 : 1);
        }

        public override string ToString()
            => (IsInherited ? $"{Offset}ms, {SliderMultiplier}x, {BeatPerMeasure}/4" : $"{Offset}ms, {Bpm}bpm, {BeatPerMeasure}/4") + (IsKiai ? " Kiai" : "");

        public static ControlPoint Parse(string line)
        {
            var values = line.Split(',');
            if (values.Length < 2) throw new InvalidOperationException($"Control point has less than the 2 required parameters: {line}");

            return new ControlPoint()
            {
                Offset = double.Parse(values[0], CultureInfo.InvariantCulture),
                beatDurationSV = double.Parse(values[1], CultureInfo.InvariantCulture),
                BeatPerMeasure = values.Length > 2 ? int.Parse(values[2]) : 4,
                SampleType = values.Length > 3 ? int.Parse(values[3]) : 1,
                SampleSet = values.Length > 4 ? (SampleSet)int.Parse(values[4]) : SampleSet.Normal,
                Volume = values.Length > 5 ? int.Parse(values[5]) : 100,
                IsInherited = values.Length > 6 ? int.Parse(values[6]) == 0 : false,
                IsKiai = values.Length > 7 ? (int.Parse(values[7]) & 1) != 0 : false,
                OmitFirstBarLine = values.Length > 7 ? (int.Parse(values[7]) & 8) != 0 : false,
            };
        }
    }
}
