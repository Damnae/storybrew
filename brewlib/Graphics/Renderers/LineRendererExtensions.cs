using OpenTK;
using OpenTK.Graphics;
using System;

namespace BrewLib.Graphics.Renderers
{
    public static class LineRendererExtensions
    {
        public static void Draw(this LineRenderer line, Vector2 from, Vector2 to, Color4 color)
            => line.Draw(new Vector3(from.X, from.Y, 0), new Vector3(to.X, to.Y, 0), color);

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
            => line.DrawCircle(new Vector3(center.X, center.Y, 0), radius, color, precision = 1);

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
    }
}
