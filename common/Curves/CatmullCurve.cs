using OpenTK;
using System;
using System.Collections.Generic;

namespace StorybrewCommon.Curves
{
    [Serializable]
    public class CatmullCurve : BaseCurve
    {
        private readonly List<Vector2> points;
        private readonly int precision;

        public override Vector2 StartPosition => points[0];
        public override Vector2 EndPosition => points[points.Count - 1];
        public bool IsLinear => points.Count < 3;

        public CatmullCurve(List<Vector2> points, int precision)
        {
            this.points = points;
            this.precision = precision;
        }

        protected override void Initialize(List<Tuple<float, Vector2>> distancePosition, out double length)
        {
            var precision = points.Count > 2 ? this.precision : 0;

            var distance = 0.0f;
            var linePrecision = precision / points.Count;
            var previousPosition = StartPosition;
            for (int lineIndex = 0; lineIndex < points.Count - 1; ++lineIndex)
            {
                for (int i = 1; i <= linePrecision; ++i)
                {
                    var delta = (float)i / (linePrecision + 1);

                    var p1 = lineIndex > 0 ? points[lineIndex - 1] : points[lineIndex];
                    var p2 = points[lineIndex];
                    var p3 = points[lineIndex + 1];
                    var p4 = lineIndex < points.Count - 2 ? points[lineIndex + 2] : points[lineIndex + 1];

                    var nextPosition = positionAtDelta(p1, p2, p3, p4, delta);

                    distance += (nextPosition - previousPosition).Length;
                    distancePosition.Add(new Tuple<float, Vector2>(distance, nextPosition));

                    previousPosition = nextPosition;
                }
            }
            distance += (EndPosition - previousPosition).Length;
            length = distance;
        }

        private Vector2 positionAtDelta(Vector2 p1, Vector2 p2, Vector2 p3, Vector2 p4, float delta)
            => 0.5f * ((-p1 + 3 * p2 - 3 * p3 + p4) * delta * delta * delta
               + (2 * p1 - 5 * p2 + 4 * p3 - p4) * delta * delta
               + (-p1 + p3) * delta
               + 2 * p2);
    }
}
