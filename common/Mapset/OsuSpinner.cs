using OpenTK;
using System;

namespace StorybrewCommon.Mapset
{
#pragma warning disable CS1591
    [Serializable]
    public class OsuSpinner : OsuHitObject
    {
        public double endTime;
        public override double EndTime => endTime;

        public static OsuSpinner Parse(string[] values, int x, int y, double startTime, HitObjectFlag flags, HitSoundAddition additions, ControlPoint timingPoint, ControlPoint controlPoint, SampleSet sampleSet, SampleSet additionsSampleSet, int customSampleSet, float volume)
        {
            var endTime = double.Parse(values[5]);

            string samplePath = string.Empty;
            if (values.Length > 6)
            {
                var special = values[6];
                var specialValues = special.Split(':');

                var objectSampleSet = (SampleSet)int.Parse(specialValues[0]);
                var objectAdditionsSampleSet = (SampleSet)int.Parse(specialValues[1]);
                var objectCustomSampleSet = 0;
                if (specialValues.Length > 2) objectCustomSampleSet = int.Parse(specialValues[2]);
                var objectVolume = 0.0f;
                if (specialValues.Length > 3) objectVolume = int.Parse(specialValues[3]);
                if (specialValues.Length > 4) samplePath = specialValues[4];

                if (objectSampleSet != 0)
                {
                    sampleSet = objectSampleSet;
                    additionsSampleSet = objectSampleSet;
                }
                if (objectAdditionsSampleSet != 0) additionsSampleSet = objectAdditionsSampleSet;
                if (objectCustomSampleSet != 0) customSampleSet = objectCustomSampleSet;
                if (objectVolume > 0.001f) volume = objectVolume;
            }
            return new OsuSpinner
            {
                PlayfieldPosition = new Vector2(x, y),
                StartTime = startTime,
                Flags = flags,
                Additions = additions,
                SampleSet = sampleSet,
                AdditionsSampleSet = additionsSampleSet,
                CustomSampleSet = customSampleSet,
                Volume = volume,
                SamplePath = samplePath,
                endTime = endTime
            };
        }
    }
}