using OpenTK;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace StorybrewCommon.Mapset
{
    [Serializable]
    public class OsuSlider : OsuHitObject
    {
        private List<OsuSliderNode> nodes;
        public IEnumerable<OsuSliderNode> Nodes => nodes;
        public int NodeCount => nodes.Count;

        private List<OsuSliderControlPoint> controlPoints;
        public IEnumerable<OsuSliderControlPoint> ControlPoints => controlPoints;
        public int ControlPointCount => controlPoints.Count;

        public override double EndTime => StartTime + (1 + TravelCount) * TravelDuration;

        public double Length;
        public double TravelDurationBeats;
        public double TravelDuration;
        public int TravelCount => nodes.Count - 1;
        public int RepeatCount => nodes.Count - 2;

        public SliderCurveType CurveType;

        public OsuSlider(List<OsuSliderNode> nodes, List<OsuSliderControlPoint> controlPoints)
        {
            this.nodes = nodes;
            this.controlPoints = controlPoints;
        }

        public override string ToString()
            => $"{base.ToString()}, {CurveType}, {TravelCount}x";

        public static OsuSlider Parse(Beatmap beatmap, string[] values, int x, int y, double startTime, HitObjectFlag flags, HitSoundAddition additions, ControlPoint timingPoint, ControlPoint controlPoint, int sampleType, int sampleAdditionsType, SampleSet sampleSet, float volume)
        {
            var slider = values[5];
            var sliderValues = slider.Split('|');

            var curveType = LetterToCurveType(sliderValues[0]);
            var sliderControlPointCount = sliderValues.Length - 1;
            var sliderControlPoints = new List<OsuSliderControlPoint>(sliderControlPointCount);
            for (var i = 0; i < sliderControlPointCount; i++)
            {
                var controlPointValues = sliderValues[i + 1].Split(':');
                var controlPointX = float.Parse(controlPointValues[0], CultureInfo.InvariantCulture);
                var controlPointY = float.Parse(controlPointValues[1], CultureInfo.InvariantCulture);
                sliderControlPoints.Add(new Vector2(controlPointX, controlPointY));
            }

            var nodeCount = int.Parse(values[6]) + 1;
            var length = double.Parse(values[7], CultureInfo.InvariantCulture);

            var sliderMultiplierLessLength = length / beatmap.SliderMultiplier;
            var travelDurationBeats = sliderMultiplierLessLength / 100 * controlPoint.SliderMultiplier;
            var travelDuration = timingPoint.BeatDuration * travelDurationBeats;

            var sliderNodes = new List<OsuSliderNode>(nodeCount);
            for (var i = 0; i < nodeCount; i++)
            {
                var nodeStartTime = startTime + i * travelDuration;
                var nodeControlPoint = beatmap.GetTimingPointAt((int)nodeStartTime);

                var node = new OsuSliderNode();
                node.SampleType = nodeControlPoint.SampleType;
                node.SampleAdditionsType = nodeControlPoint.SampleType;
                node.SampleSet = nodeControlPoint.SampleSet;
                node.Volume = nodeControlPoint.Volume;
                node.Additions = additions;
                sliderNodes.Add(node);
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
                    var nodeSampleType = int.Parse(sampleAndAdditionTypeValues2[0]);
                    var nodeSampleAdditionsType = int.Parse(sampleAndAdditionTypeValues2[1]);

                    if (nodeSampleType != 0)
                        node.SampleType = nodeSampleType;
                    if (nodeSampleAdditionsType != 0)
                        node.SampleAdditionsType = nodeSampleAdditionsType;
                }
            }

            return new OsuSlider(sliderNodes, sliderControlPoints)
            {
                PlayfieldPosition = new Vector2(x, y),
                StartTime = startTime,
                Flags = flags,
                Additions = additions,
                SampleType = sampleType,
                SampleAdditionsType = sampleAdditionsType,
                SampleSet = sampleSet,
                Volume = volume,
                SamplePath = string.Empty,
                // Slider specific
                CurveType = curveType,
                Length = length,
                TravelDurationBeats = travelDurationBeats,
                TravelDuration = travelDuration,
            };
        }

        public static SliderCurveType LetterToCurveType(string letter)
        {
            switch (letter)
            {
                case "L": return SliderCurveType.Linear;
                case "C": return SliderCurveType.Catmull;
                case "B": return SliderCurveType.Bezier;
                case "P": return SliderCurveType.Perfect;
                default: return SliderCurveType.Unknown;
            }
        }
    }

    [Serializable]
    public class OsuSliderNode
    {
        public HitSoundAddition Additions;
        public int SampleType;
        public int SampleAdditionsType;
        public SampleSet SampleSet;
        public float Volume;
    }

    [Serializable]
    public struct OsuSliderControlPoint
    {
        public Vector2 PlayfieldPosition;
        public Vector2 Position => PlayfieldPosition + OsuHitObject.PlayfieldToStoryboardOffset;

        public static implicit operator OsuSliderControlPoint(Vector2 vector2)
            => new OsuSliderControlPoint() { PlayfieldPosition = vector2, };
    }

    public enum SliderCurveType
    {
        Unknown,
        Linear,
        Catmull,
        Bezier,
        Perfect,
    }
}
