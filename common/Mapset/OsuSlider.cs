using OpenTK;
using StorybrewCommon.Curves;
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

        public override double EndTime => StartTime + TravelCount * TravelDuration;

        private Curve curve;

        private Vector2 playfieldTipPosition;
        public Vector2 PlayfieldTipPosition
        {
            get
            {
                if (curve == null) generateCurve();
                return playfieldTipPosition;
            }
        }

        public Vector2 TipPosition => PlayfieldTipPosition + PlayfieldToStoryboardOffset;

        /// <summary>
        /// The total distance the slider ball travels, in osu!pixels.
        /// </summary>
        public double Length;

        /// <summary>
        /// The time it takes for the slider ball to travels across the slider's body in beats.
        /// </summary>
        public double TravelDurationBeats;

        /// <summary>
        /// The time it takes for the slider ball to travels across the slider's body in milliseconds.
        /// </summary>
        public double TravelDuration;

        /// <summary>
        /// How many times the slider ball travels across the slider's body.
        /// </summary>
        public int TravelCount => nodes.Count - 1;

        /// <summary>
        /// How many times the slider ball hits a repeat.
        /// </summary>
        public int RepeatCount => nodes.Count - 2;

        public SliderCurveType CurveType;

        public OsuSlider(List<OsuSliderNode> nodes, List<OsuSliderControlPoint> controlPoints)
        {
            this.nodes = nodes;
            this.controlPoints = controlPoints;
        }

        public override Vector2 PlayfieldPositionAtTime(double time)
        {
            if (time <= StartTime)
                return PlayfieldPosition;

            if (EndTime <= time)
                return TravelCount % 2 == 0 ? PlayfieldPosition : PlayfieldTipPosition;

            var elapsedSinceStartTime = time - StartTime;

            var repeatAtTime = 1;
            var progressDuration = elapsedSinceStartTime;
            while (progressDuration > TravelDuration)
            {
                progressDuration -= TravelDuration;
                ++repeatAtTime;
            }

            var progress = progressDuration / TravelDuration;
            var reversed = repeatAtTime % 2 == 0;
            if (reversed) progress = 1.0 - progress;

            if (curve == null) generateCurve();
            return curve.PositionAtDistance(Length * progress);
        }

        public override string ToString()
            => $"{base.ToString()}, {CurveType}, {TravelCount}x";

        private void generateCurve()
        {
            switch (CurveType)
            {
                case SliderCurveType.Catmull:
                    if (controlPoints.Count == 1) goto case SliderCurveType.Linear;
                    curve = generateCatmullCurve();
                    break;
                case SliderCurveType.Bezier:
                    if (controlPoints.Count == 1) goto case SliderCurveType.Linear;
                    curve = generateBezierCurve();
                    break;
                case SliderCurveType.Perfect:
                    if (controlPoints.Count < 2) goto case SliderCurveType.Linear;
                    if (controlPoints.Count > 2) goto case SliderCurveType.Bezier;
                    curve = generateCircleCurve();
                    break;
                case SliderCurveType.Linear:
                default:
                    curve = generateLinearCurve();
                    break;
            }
            playfieldTipPosition = curve.PositionAtDistance(Length);
        }

        private Curve generateCircleCurve()
            => new CircleCurve(PlayfieldPosition, controlPoints[0].PlayfieldPosition, controlPoints[1].PlayfieldPosition);

        private Curve generateBezierCurve()
        {
            var curves = new List<Curve>();

            var curvePoints = new List<Vector2>();
            var precision = (int)Math.Ceiling(Length);

            var previousPosition = PlayfieldPosition;
            curvePoints.Add(previousPosition);

            foreach (var controlPoint in controlPoints)
            {
                if (controlPoint.PlayfieldPosition == previousPosition)
                {
                    if (curvePoints.Count > 1)
                        curves.Add(new Curves.BezierCurve(curvePoints, precision));

                    curvePoints = new List<Vector2>();
                }

                curvePoints.Add(controlPoint.PlayfieldPosition);
                previousPosition = controlPoint.PlayfieldPosition;
            }

            if (curvePoints.Count > 1)
                curves.Add(new Curves.BezierCurve(curvePoints, precision));

            return new CompositeCurve(curves);
        }

        private Curve generateCatmullCurve()
        {
            List<Vector2> curvePoints = new List<Vector2>(controlPoints.Count + 1);
            curvePoints.Add(PlayfieldPosition);
            foreach (var controlPoint in controlPoints)
                curvePoints.Add(controlPoint.PlayfieldPosition);

            var precision = (int)Math.Ceiling(Length);
            return new CatmullCurve(curvePoints, precision);
        }

        private Curve generateLinearCurve()
        {
            var curves = new List<Curve>();

            var previousPoint = PlayfieldPosition;
            foreach (var controlPoint in controlPoints)
            {
                curves.Add(new Curves.BezierCurve(new List<Vector2>()
                {
                    previousPoint,
                    controlPoint.PlayfieldPosition,
                }, 0));
                previousPoint = controlPoint.PlayfieldPosition;
            }
            return new CompositeCurve(curves);
        }

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
                sliderNodes.Add(new OsuSliderNode()
                {
                    Time = nodeStartTime,
                    SampleType = nodeControlPoint.SampleType,
                    SampleAdditionsType = nodeControlPoint.SampleType,
                    SampleSet = nodeControlPoint.SampleSet,
                    Volume = nodeControlPoint.Volume,
                    Additions = additions,
                });
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
        public double Time;
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
