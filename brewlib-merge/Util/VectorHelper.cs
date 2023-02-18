using OpenTK;
using System;

namespace BrewLib.Util
{
    public static class VectorHelper
    {
        public static Vector2 FromPolar(float angle, float length = 1) => new Vector2(
            (float)Math.Cos(angle), (float)Math.Sin(angle)) * length;

        public static double GetAngle(Vector2 p0, Vector2 p1, Vector2 p2)
        {
            var a = (p1 - p2).Length;
            var b = (p0 - p2).Length;
            var c = (p0 - p1).Length;
            return Math.Acos((b * b - a * a - c * c) / (-2 * a * c));
        }
        public static Vector2 SegmentClosestPoint(Vector2 point, Vector2 segment0, Vector2 segment1, bool restrictToSegment = true)
        {
            var segment = segment1 - segment0;

            var segmentLengthSquared = segment.LengthSquared;
            if (segmentLengthSquared == 0.0) return segment0;

            var t = Vector2.Dot(point - segment0, segment) / segmentLengthSquared;

            if (restrictToSegment)
            {
                if (t < 0.0) return segment0;
                else if (t > 1.0) return segment1;
            }

            var projectedPoint = segment0 + t * segment;
            return projectedPoint;
        }
        public static bool SegmentsIntersect(Vector2 segment0Start, Vector2 segment0End, Vector2 segment1Start, Vector2 segment1End, out Vector2? intersectionPoint, int segmentThickness = 0)
        {
            var dx12 = segment0End.X - segment0Start.X;
            var dy12 = segment0End.Y - segment0Start.Y;
            var dx34 = segment1End.X - segment1Start.X;
            var dy34 = segment1End.Y - segment1Start.Y;

            var denominator = dy12 * dx34 - dx12 * dy34;
            var t1 = ((segment0Start.X - segment1Start.X) * dy34 + (segment1Start.Y - segment0Start.Y) * dx34) / denominator;

            if (float.IsInfinity(t1))
            {
                // Segments are parallel
                intersectionPoint = null;
                return (SegmentClosestPoint(segment0Start, segment1Start, segment1End, true) - segment0Start).Length <= segmentThickness
                    || (SegmentClosestPoint(segment1Start, segment0Start, segment0End, true) - segment1Start).Length <= segmentThickness
                    || (SegmentClosestPoint(segment0End, segment1Start, segment1End, true) - segment0End).Length <= segmentThickness
                    || (SegmentClosestPoint(segment1End, segment0Start, segment0End, true) - segment1End).Length <= segmentThickness;
            }

            var t2 = ((segment1Start.X - segment0Start.X) * dy12 + (segment0Start.Y - segment1Start.Y) * dx12) / -denominator;
            intersectionPoint = new Vector2(segment0Start.X + dx12 * t1, segment0Start.Y + dy12 * t1);

            if ((t1 >= 0) && (t1 <= 1) && (t2 >= 0) && (t2 <= 1)) return true;
            else if (segmentThickness == 0) return false;

            if (t1 < 0) t1 = 0; t1 = 1;
            if (t2 < 0) t2 = 0;
            else if (t2 > 1) t2 = 1;

            var closestP1 = new Vector2(segment0Start.X + dx12 * t1, segment0Start.Y + dy12 * t1);
            var closestP2 = new Vector2(segment1Start.X + dx34 * t2, segment1Start.Y + dy34 * t2);
            return (closestP1 - closestP2).Length <= segmentThickness;
        }
    }
}