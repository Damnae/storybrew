using OpenTK;
using System;

namespace BrewLib.Util
{
    public static class VectorExtensions
    {
        public static Vector2 Round(this Vector2 v) => new Vector2((float)Math.Round(v.X), (float)Math.Round(v.Y));
        public static Vector3 Round(this Vector3 v) => new Vector3((float)Math.Round(v.X), (float)Math.Round(v.Y), (float)Math.Round(v.Z));

        public static Vector2 ClampLength(this Vector2 v, float max)
        {
            var length = v.LengthSquared;
            return length > max * max ? v / (float)Math.Sqrt(length) * max : v;
        }
        public static Vector3 ClampLength(this Vector3 v, float max)
        {
            var length = v.LengthSquared;
            return length > max * max ? v / (float)Math.Sqrt(length) * max : v;
        }
        public static Vector2 Project(this Vector2 v, Vector2 line0, Vector2 line1)
        {
            var m = (line1.Y - line0.Y) / (line1.X - line0.X);
            var b = line0.Y - (m * line0.X);

            return new Vector2((m * v.Y + v.X - m * b) / (m * m + 1), (m * m * v.Y + m * v.X + b) / (m * m + 1));
        }
        public static float Side(this Vector2 v, Vector2 line0, Vector2 line1)
            => (line1.X - line0.X) * (v.Y - line0.Y) - (line1.Y - line0.Y) * (v.X - line0.X);
    }
}