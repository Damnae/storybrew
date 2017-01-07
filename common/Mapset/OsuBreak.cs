using System;

namespace StorybrewCommon.Mapset
{
    [Serializable]
    public class OsuBreak
    {
        public double StartTime;
        public double EndTime;

        public override string ToString()
            => $"Break from {StartTime}ms to {EndTime}ms";

        public static OsuBreak Parse(Beatmap beatmap, string line)
        {
            var values = line.Split(',');
            return new OsuBreak()
            {
                StartTime = int.Parse(values[1]),
                EndTime = int.Parse(values[2]),
            };
        }
    }
}