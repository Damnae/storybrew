using OpenTK;
using OpenTK.Graphics;
using System;

namespace BrewLib.Graphics.Renderers
{
    public static class LineRendererExtensions
    {
        public static void Draw(this LineRenderer line, Vector2 from, Vector2 to, Color4 color)
            => line.Draw(new Vector3(from.X, from.Y, 0), new Vector3(to.X, to.Y, 0), color);

        public static void Draw(this LineRenderer line, Vector2 from, Vector2 to, Color4 startColor, Color4 endColor)
            => line.Draw(new Vector3(from.X, from.Y, 0), new Vector3(to.X, to.Y, 0), startColor, endColor);

        public static void DrawCircle(this LineRenderer line, Vector3 center, float radius, Color4 color, float precision = 1)
        {
            var circumference = MathHelper.TwoPi * radius;
            var lineCount = Math.Max(16, (int)Math.Round(circumference * precision));

            var angleStep = MathHelper.TwoPi / lineCount;
            var previousPosition = new Vector3(center.X + radius, center.Y, center.Z);
            for (var i = 1; i <= lineCount; i++)
            {
                var angle = angleStep * i;
                var position = new Vector3(center.X + (float)Math.Cos(angle) * radius, center.Y + (float)Math.Sin(angle) * radius, center.Z);
                line.Draw(previousPosition, position, color);
                previousPosition = position;
            }
        }

        public static void DrawCircle(this LineRenderer line, Vector2 center, float radius, Color4 color, float precision = 1)
            => line.DrawCircle(new Vector3(center.X, center.Y, 0), radius, color, precision);

        public static void DrawSquare(this LineRenderer line, Vector3 from, Vector3 to, Color4 color)
        {
            var topLeft = from;
            var topRight = new Vector3(to.X, from.Y, from.Z);
            var bottomLeft = new Vector3(from.X, to.Y, from.Z);
            var bottomRight = to;

            line.Draw(topLeft, topRight, color);
            line.Draw(topRight, bottomRight, color);
            line.Draw(bottomRight, bottomLeft, color);
            line.Draw(bottomLeft, topLeft, color);
        }

        public static void DrawSquare(this LineRenderer line, Box2 box, Color4 color)
            => line.DrawSquare(new Vector3(box.Left, box.Top, 0), new Vector3(box.Right, box.Bottom, 0), color);

        public static void DrawSquare(this LineRenderer line, Vector3 at, Vector2 size, Color4 color)
            => line.DrawSquare(new Vector3(at.X - size.X * 0.5f, at.Y - size.Y * 0.5f, at.Z), new Vector3(at.X + size.X * 0.5f, at.Y + size.Y * 0.5f, at.Z), color);

        public static void DrawSquare(this LineRenderer line, Vector2 at, Vector2 size, Color4 color)
            => line.DrawSquare(new Vector3(at.X - size.X * 0.5f, at.Y - size.Y * 0.5f, 0), new Vector3(at.X + size.X * 0.5f, at.Y + size.Y * 0.5f, 0), color);

        public static void DrawCone(this LineRenderer line, Vector3 center, float arc, float orientation, float innerRadius, float radius, Color4 color, float precision = 1)
        {
            var fromAngle = orientation - arc * 0.5f;
            var toAngle = orientation + arc * 0.5f;

            line.Draw(center + new Vector3((float)Math.Cos(fromAngle) * innerRadius, (float)Math.Sin(fromAngle) * innerRadius, 0),
                center + new Vector3((float)Math.Cos(fromAngle) * radius, (float)Math.Sin(fromAngle) * radius, 0), color);
            line.Draw(center + new Vector3((float)Math.Cos(toAngle) * innerRadius, (float)Math.Sin(toAngle) * innerRadius, 0),
                center + new Vector3((float)Math.Cos(toAngle) * radius, (float)Math.Sin(toAngle) * radius, 0), color);

            var minLineCount = Math.Max((int)Math.Round(16 * (arc / MathHelper.TwoPi)), 2);

            {
                var outerCircumference = arc * radius;
                var outerLineCount = Math.Max(minLineCount, (int)Math.Round(outerCircumference * precision));

                var angleStep = arc / outerLineCount;
                var previousPosition = center + new Vector3((float)Math.Cos(fromAngle) * radius, (float)Math.Sin(fromAngle) * radius, 0);

                for (var i = 1; i <= outerLineCount; i++)
                {
                    var angle = fromAngle + angleStep * i;
                    var position = new Vector3(center.X + (float)Math.Cos(angle) * radius, center.Y + (float)Math.Sin(angle) * radius, center.Z);
                    line.Draw(previousPosition, position, color);
                    previousPosition = position;
                }
            }
            if (innerRadius > 0)
            {
                var innerCircumference = arc * innerRadius;
                var innerLineCount = Math.Max(minLineCount, (int)Math.Round(innerCircumference * precision));

                var angleStep = arc / innerLineCount;
                var previousPosition = new Vector3(center.X + (float)Math.Cos(fromAngle) * innerRadius, center.Y + (float)Math.Sin(fromAngle) * innerRadius, center.Z);
                for (var i = 1; i <= innerLineCount; i++)
                {
                    var angle = fromAngle + angleStep * i;
                    var position = new Vector3(center.X + (float)Math.Cos(angle) * innerRadius, center.Y + (float)Math.Sin(angle) * innerRadius, center.Z);
                    line.Draw(previousPosition, position, color);
                    previousPosition = position;
                }
            }
        }

        public static void DrawCone(this LineRenderer line, Vector2 center, float arc, float orientation, float innerRadius, float radius, Color4 color, float precision = 1)
            => line.DrawCone(new Vector3(center.X, center.Y, 0), arc, orientation, innerRadius, radius, color, precision);
    }
}