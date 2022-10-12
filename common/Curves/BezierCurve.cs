using OpenTK;
using System;
using System.Collections.Generic;

namespace StorybrewCommon.Curves
{
    [Serializable]
    public class BezierCurve : BaseCurve
    {
        private readonly List<Vector2> points;
        private readonly int precision;

        public override Vector2 StartPosition => points[0];
        public override Vector2 EndPosition => points[points.Count - 1];
        public bool IsLinear => points.Count < 3;

        public BezierCurve(List<Vector2> points, int precision)
        {
            this.points = points;
            this.precision = precision;
        }

        protected override void Initialize(List<ValueTuple<float, Vector2>> distancePosition, out double length)
        {
            var precision = points.Count > 2 ? this.precision : 0;

            var distance = 0.0f;
            var previousPosition = StartPosition;
            for (var i = 1; i <= precision; ++i)
            {
                var delta = (float)i / (precision + 1);
                var nextPosition = positionAtDelta(delta);

                distance += (nextPosition - previousPosition).Length;
                distancePosition.Add(new ValueTuple<float, Vector2>(distance, nextPosition));

                previousPosition = nextPosition;
            }
            distance += (EndPosition - previousPosition).Length;
            length = distance;

        }

        [ThreadStatic] private static Vector2[] intermediatePoints;

        private Vector2 positionAtDelta(float delta)
        {
            var pointsCount = points.Count;

            if (intermediatePoints == null || intermediatePoints.Length < pointsCount)
                intermediatePoints = new Vector2[pointsCount];

            for (var i = 0; i < pointsCount; ++i)
                intermediatePoints[i] = points[i];

            for (var i = 1; i < pointsCount; ++i)
                for (var j = 0; j < pointsCount - i; ++j)
                    intermediatePoints[j] =
                        intermediatePoints[j] * (1 - delta) +
                        intermediatePoints[j + 1] * delta;

            return intermediatePoints[0];
        }
    }
}
