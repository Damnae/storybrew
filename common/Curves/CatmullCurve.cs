using OpenTK;
using System;
using System.Collections.Generic;

namespace StorybrewCommon.Curves
{
    ///<summary> Represents a Catmull-Rom spline curve. </summary>
    [Serializable] public class CatmullCurve : BaseCurve
    {
        readonly List<Vector2> points;
        readonly int precision;

        ///<inheritdoc/>
        public override Vector2 StartPosition => points[0];

        ///<inheritdoc/>
        public override Vector2 EndPosition => points[points.Count - 1];

        ///<summary> Whether the curve is straight (linear). </summary>
        public bool IsLinear => points.Count < 3;

        ///<summary> Constructs a Catmull-Rom curve from given control points <paramref name="points"/>. </summary>
        public CatmullCurve(List<Vector2> points, int precision)
        {
            this.points = points;
            this.precision = precision;
        }

        ///<summary/>
        protected override void Initialize(List<ValueTuple<float, Vector2>> distancePosition, out double length)
        {
            var precision = points.Count > 2 ? this.precision : 0;

            var distance = 0f;
            var linePrecision = precision / points.Count;
            var previousPosition = StartPosition;

            for (var lineIndex = 0; lineIndex < points.Count - 1; ++lineIndex)
            {
                for (var i = 1; i <= linePrecision; ++i)
                {
                    var delta = (float)i / (linePrecision + 1);

                    var p1 = lineIndex > 0 ? points[lineIndex - 1] : points[lineIndex];
                    var p2 = points[lineIndex];
                    var p3 = points[lineIndex + 1];
                    var p4 = lineIndex < points.Count - 2 ? points[lineIndex + 2] : points[lineIndex + 1];

                    var nextPosition = positionAtDelta(p1, p2, p3, p4, delta);

                    distance += (nextPosition - previousPosition).Length;
                    distancePosition.Add(new ValueTuple<float, Vector2>(distance, nextPosition));

                    previousPosition = nextPosition;
                }
            }
            distance += (EndPosition - previousPosition).Length;
            length = distance;
        }

        Vector2 positionAtDelta(Vector2 p1, Vector2 p2, Vector2 p3, Vector2 p4, float delta)
            => ((-p1 + 3 * p2 - 3 * p3 + p4) * delta * delta * delta
            + (2 * p1 - 5 * p2 + 4 * p3 - p4) * delta * delta + (-p1 + p3) * delta + 2 * p2) / 2;
    }
}