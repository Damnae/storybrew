using OpenTK;
using System;
using System.Globalization;

namespace StorybrewCommon.Mapset
{
    [Serializable]
    public class OsuSpinner : OsuHitObject
    {
        public double endTime;
        public override double EndTime => endTime;

        public static OsuSpinner Parse(Beatmap beatmap, string[] values, int x, int y, double startTime, HitObjectFlag flags, HitSoundAddition additions, ControlPoint timingPoint, ControlPoint controlPoint, int sampleType, int sampleAdditionsType, SampleSet sampleSet, float volume)
        {
            var endTime = double.Parse(values[5], CultureInfo.InvariantCulture);

            string samplePath = string.Empty;
            if (values.Length > 6)
            {
                var special = values[6];
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
            return new OsuSpinner()
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
                // Spinner specific
                endTime = endTime,
            };
        }
    }
}
