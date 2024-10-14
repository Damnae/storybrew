using OpenTK;

namespace StorybrewCommon.Mapset
{
    [Serializable]
    public class OsuCircle : OsuHitObject
    {
        public static OsuCircle Parse(Beatmap beatmap, string[] values, int x, int y, double startTime, HitObjectFlag flags, HitSoundAddition additions, ControlPoint timingPoint, ControlPoint controlPoint, SampleSet sampleSet, SampleSet additionsSampleSet, int customSampleSet, float volume)
        {
            string samplePath = string.Empty;
            if (values.Length > 5)
            {
                var special = values[5];
                var specialValues = special.Split(':');
                var objectSampleSet = (SampleSet)int.Parse(specialValues[0]);
                var objectAdditionsSampleSet = (SampleSet)int.Parse(specialValues[1]);
                var objectCustomSampleSet = 0;
                if (specialValues.Length > 2)
                    objectCustomSampleSet = int.Parse(specialValues[2]);
                var objectVolume = 0.0f;
                if (specialValues.Length > 3)
                    objectVolume = int.Parse(specialValues[3]);
                if (specialValues.Length > 4)
                    samplePath = specialValues[4];

                if (objectSampleSet != 0)
                {
                    sampleSet = objectSampleSet;
                    additionsSampleSet = objectSampleSet;
                }
                if (objectAdditionsSampleSet != 0)
                    additionsSampleSet = objectAdditionsSampleSet;
                if (objectCustomSampleSet != 0)
                    customSampleSet = objectCustomSampleSet;
                if (objectVolume > 0.001f)
                    volume = objectVolume;
            }
            return new OsuCircle()
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
                // Circle specific
            };
        }
    }
}
