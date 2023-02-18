using OpenTK;
using System;
using System.Collections.Generic;

namespace StorybrewCommon.Curves
{
#pragma warning disable CS1591
    [Serializable]
    public class BezierCurve : BaseCurve
    {
        readonly List<Vector2> points;
        readonly int precision;

        ///<summary> The start position (the head) of the bézier curve. </summary>
        public override Vector2 StartPosition => points[0];

        ///<summary> The end position (the tail) of the bézier curve. </summary>
        public override Vector2 EndPosition => points[points.Count - 1];

        ///<summary> Whether the bézier curve is straight (linear). </summary>
        public bool IsLinear => points.Count < 3;

        ///<summary> Constructs a bézier curve from a list of points <paramref name="points"/>. </summary>
        public BezierCurve(List<Vector2> points, int precision)
        {
            this.points = points;
            this.precision = precision;
        }

        protected override void Initialize(List<ValueTuple<float, Vector2>> distancePosition, out double length)
        {
            var precision = points.Count > 2 ? this.precision : 0;

            var distance = 0f;
            var previousPosition = StartPosition;

            for (var i = 1f; i <= precision; ++i)
            {
                var delta = i / (precision + 1);
                var nextPosition = positionAtDelta(delta);

                distance += (nextPosition - previousPosition).Length;
                distancePosition.Add(new ValueTuple<float, Vector2>(distance, nextPosition));

                previousPosition = nextPosition;
            }
            distance += (EndPosition - previousPosition).Length;
            length = distance;
        }

        [ThreadStatic] static Vector2[] intermediatePoints;

        Vector2 positionAtDelta(float delta)
        {
            var pointsCount = points.Count;

            if (intermediatePoints == null || intermediatePoints.Length < pointsCount) intermediatePoints = new Vector2[pointsCount];

            for (var i = 0; i < pointsCount; ++i) intermediatePoints[i] = points[i];
            for (var i = 1; i < pointsCount; ++i) for (var j = 0; j < pointsCount - i; ++j) intermediatePoints[j] =
                intermediatePoints[j] * (1 - delta) + intermediatePoints[j + 1] * delta;

            return intermediatePoints[0];
        }
    }
}