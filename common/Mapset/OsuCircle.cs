using OpenTK;
using System;

namespace StorybrewCommon.Mapset
{
    [Serializable]
    public class OsuCircle : OsuHitObject
    {
        public static OsuCircle Parse(Beatmap beatmap, string[] values, int x, int y, double startTime, HitObjectFlag flags, HitSoundAddition additions, ControlPoint timingPoint, ControlPoint controlPoint, int sampleType, int sampleAdditionsType, SampleSet sampleSet, float volume)
        {
            string samplePath = string.Empty;
            if (values.Length > 5)
            {
                var special = values[5];
                var specialValues = special.Split(':');
                var objectSampleType = int.Parse(specialValues[0]);
                var objectSampleAdditionsType = int.Parse(specialValues[1]);
                var objectSampleSet = (SampleSet)int.Parse(specialValues[2]);
                var objectVolume = 0.0f;
                if (specialValues.Length > 3)
                    objectVolume = int.Parse(specialValues[3]);
                if (specialValues.Length > 4)
                    samplePath = specialValues[4];

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
            }
            return new OsuCircle()
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
                // Circle specific
            };
        }
    }
}
