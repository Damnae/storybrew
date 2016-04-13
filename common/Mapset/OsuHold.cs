using OpenTK;
using System;
using System.Globalization;

namespace StorybrewCommon.Mapset
{
    [Serializable]
    public class OsuHold : OsuHitObject
    {
        public double endTime;
        public override double EndTime => endTime;

        public static OsuHold Parse(Beatmap beatmap, string[] values, int x, int y, double startTime, HitObjectFlag flags, HitSoundAddition additions, ControlPoint timingPoint, ControlPoint controlPoint, int sampleType, int sampleAdditionsType, SampleSet sampleSet, float volume)
        {
            string samplePath = string.Empty;

            var special = values[5];
            var specialValues = special.Split(':');

            var endTime = double.Parse(specialValues[0], CultureInfo.InvariantCulture);
            var objectSampleType = int.Parse(specialValues[1]);
            var objectSampleAdditionsType = int.Parse(specialValues[2]);
            var objectSampleSet = (SampleSet)int.Parse(specialValues[3]);
            var objectVolume = 0.0f;
            if (specialValues.Length > 4)
                objectVolume = int.Parse(specialValues[4]);
            if (specialValues.Length > 5)
                samplePath = specialValues[5];

            if (objectSampleType != 0)
            {
                sampleType = objectSampleType;
                sampleAdditionsType = objectSampleType;
            }
            if (objectSampleAdditionsType != 0)
                sampleAdditionsType = objectSampleAdditionsType;
            if (objectSampleSet != 0)
                sampleSet = objectSampleSet;
            if (objectVolume > 0.001f)
                volume = objectVolume;

            return new OsuHold()
            {
                PlayfieldPosition = new Vector2(x, y),
                StartTime = startTime,
                Flags = flags,
                Additions = additions,
                SampleType = sampleType,
                SampleAdditionsType = sampleAdditionsType,
                SampleSet = sampleSet,
                Volume = volume,
                SamplePath = samplePath,
                // Hold specific
                endTime = endTime,
            };
        }
    }
}
