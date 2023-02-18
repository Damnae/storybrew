using System;

namespace StorybrewCommon.Mapset
{
#pragma warning disable CS1591
    [Serializable] public class OsuBreak
    {
        public double StartTime, EndTime;

        public override string ToString() => $"Break from {StartTime}ms to {EndTime}ms";
        public static OsuBreak Parse(string line)
        {
            var values = line.Split(',');
            return new OsuBreak
            {
                StartTime = int.Parse(values[1]),
                EndTime = int.Parse(values[2])
            };
        }
    }
}