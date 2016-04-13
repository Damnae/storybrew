using OpenTK;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace StorybrewCommon.Mapset
{
    [Serializable]
    public class OsuHitObject
    {
        public static readonly Vector2 PlayfieldSize = new Vector2(512, 384);
        public static readonly Vector2 StoryboardSize = new Vector2(640, 480);
        public static readonly Vector2 PlayfieldToStoryboardOffset = (StoryboardSize - PlayfieldSize) * 0.5f;

        public Vector2 PlayfieldPosition;
        public Vector2 Position => PlayfieldPosition + PlayfieldToStoryboardOffset;

        public double StartTime;
        public HitObjectFlag Flags;
        public HitSoundAddition Additions;
        public int SampleType;
        public int SampleAdditionsType;
        public int SampleSet;
        public float Volume;
        public string SamplePath;

        public static OsuHitObject Parse(string line, Beatmap beatmap)
        {
            var values = line.Split(',');

            var x = int.Parse(values[0]);
            var y = int.Parse(values[1]);
            var startTime = double.Parse(values[2], CultureInfo.InvariantCulture);
            var flags = (HitObjectFlag)int.Parse(values[3]);
            var additions = (HitSoundAddition)int.Parse(values[4]);

            var timingPoint = beatmap.GetTimingPointAt((int)startTime);
            var controlPoint = beatmap.GetControlPointAt((int)startTime);

            var sampleType = controlPoint.SampleType;
            var sampleAdditionsType = controlPoint.SampleType;
            var sampleSet = controlPoint.SampleSet;
            var volume = controlPoint.Volume;

            if (flags.HasFlag(HitObjectFlag.Circle))
            {
                string samplePath = null;
                if (values.Length > 5)
                {
                    var special = values[5];
                    var specialValues = special.Split(':');
                    var objectSampleType = int.Parse(specialValues[0]);
                    var objectSampleAdditionsType = int.Parse(specialValues[1]);
                    var objectSampleSet = int.Parse(specialValues[2]);
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
            else if (flags.HasFlag(HitObjectFlag.Slider))
            {
                var slider = values[5];
                var sliderValues = slider.Split('|');

                var nodeCount = int.Parse(values[6]) + 1;
                var length = double.Parse(values[7], CultureInfo.InvariantCulture);

                var sliderMultiplierLessLength = length / beatmap.SliderMultiplier;
                var lengthInBeats = sliderMultiplierLessLength / 100 * controlPoint.SliderMultiplier;
                var repeatDuration = timingPoint.BeatDuration * lengthInBeats;

                var sliderNodes = new List<OsuSliderNode>(nodeCount);
                var endTime = startTime;

                for (var i = 0; i < nodeCount; i++)
                {
                    var nodeStartTime = startTime + i * repeatDuration;
                    var nodeControlPoint = beatmap.GetTimingPointAt((int)nodeStartTime);

                    var node = new OsuSliderNode();
                    node.SampleType = nodeControlPoint.SampleType;
                    node.SampleAdditionsType = nodeControlPoint.SampleType;
                    node.SampleSet = nodeControlPoint.SampleSet;
                    node.Volume = nodeControlPoint.Volume;
                    node.Additions = additions;
                    sliderNodes.Add(node);

                    endTime = nodeStartTime;
                }

                if (values.Length > 8)
                {
                    var sliderAddition = values[8];
                    var sliderAdditionValues = sliderAddition.Split('|');
                    for (var i = 0; i < sliderAdditionValues.Length; i++)
                    {
                        var node = sliderNodes[i];
                        var nodeAdditions = (HitSoundAddition)int.Parse(sliderAdditionValues[i]);
                        node.Additions = nodeAdditions;
                    }
                }

                if (values.Length > 9)
                {
                    var sampleAndAdditionType = values[9];
                    var sampleAndAdditionTypeValues = sampleAndAdditionType.Split('|');
                    for (var i = 0; i < sampleAndAdditionTypeValues.Length; i++)
                    {
                        var node = sliderNodes[i];
                        var sampleAndAdditionTypeValues2 = sampleAndAdditionTypeValues[i].Split(':');
                        int nodeSampleType = int.Parse(sampleAndAdditionTypeValues2[0]);
                        int nodeSampleAdditionsType = int.Parse(sampleAndAdditionTypeValues2[1]);

                        if (nodeSampleType != 0)
                            node.SampleType = nodeSampleType;
                        if (nodeSampleAdditionsType != 0)
                            node.SampleAdditionsType = nodeSampleAdditionsType;
                    }
                }

                return new OsuSlider(sliderNodes)
                {
                    PlayfieldPosition = new Vector2(x, y),
                    StartTime = startTime,
                    Flags = flags,
                    Additions = additions,
                    SampleType = sampleType,
                    SampleAdditionsType = sampleAdditionsType,
                    SampleSet = sampleSet,
                    Volume = volume,
                    // Slider specific
                    EndTime = endTime,
                    Length = length,
                    SVLessLength = sliderMultiplierLessLength,
                    LengthInBeats = lengthInBeats,
                    RepeatDuration = repeatDuration,
                };
            }
            else if (flags.HasFlag(HitObjectFlag.Hold))
            {
                string samplePath = null;

                var special = values[5];
                var specialValues = special.Split(':');

                var endTime = double.Parse(specialValues[0], CultureInfo.InvariantCulture);
                var objectSampleType = int.Parse(specialValues[1]);
                var objectSampleAdditionsType = int.Parse(specialValues[2]);
                var objectSampleSet = int.Parse(specialValues[3]);
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
                    // Hold specific
                    EndTime = endTime,
                };
            }
            else if (flags.HasFlag(HitObjectFlag.Spinner))
            {
                var endTime = double.Parse(values[5], CultureInfo.InvariantCulture);

                string samplePath = null;
                if (values.Length > 6)
                {
                    var special = values[6];
                    var specialValues = special.Split(':');
                    var objectSampleType = int.Parse(specialValues[0]);
                    var objectSampleAdditionsType = int.Parse(specialValues[1]);
                    var objectSampleSet = int.Parse(specialValues[2]);
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
                    // Spinner specific
                    EndTime = endTime,
                };
            }
            return null;
        }
    }

    [Serializable]
    public class OsuCircle : OsuHitObject
    {
    }

    [Serializable]
    public class OsuSlider : OsuHitObject
    {
        private List<OsuSliderNode> nodes;

        public IEnumerable<OsuSliderNode> Nodes => nodes;
        public double EndTime;
        public double Length;
        public double SVLessLength;
        public double LengthInBeats;
        public double RepeatDuration;

        public OsuSlider(List<OsuSliderNode> nodes)
        {
            this.nodes = nodes;
        }
    }

    [Serializable]
    public class OsuHold : OsuHitObject
    {
        public double EndTime;
    }

    [Serializable]
    public class OsuSpinner : OsuHitObject
    {
        public double EndTime;
    }

    [Serializable]
    public class OsuSliderNode
    {
        public HitSoundAddition Additions;
        public int SampleAdditionsType;
        public int SampleSet;
        public int SampleType;
        public float Volume;
    }

    [Flags]
    public enum HitObjectFlag
    {
        Circle = 1,
        Slider = 2,
        NewCombo = 4,
        Spinner = 8,
        Colors = 112,
        Hold = 128
    }

    [Flags]
    public enum HitSoundAddition
    {
        None = 0,
        Normal = 1,
        Whistle = 2,
        Finish = 4,
        Clap = 8
    }
}
