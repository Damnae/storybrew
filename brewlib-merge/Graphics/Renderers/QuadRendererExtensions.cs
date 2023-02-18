using BrewLib.Graphics.Textures;
using BrewLib.Util;
using OpenTK;
using OpenTK.Graphics;
using System;

namespace BrewLib.Graphics.Renderers
{
    public static class QuadRendererExtensions
    {
        public static void Draw(this QuadRenderer renderer, Texture2dRegion texture, float x, float y, float originX, float originY, float scaleX, float scaleY, float rotation, Color4 color)
            => renderer.Draw(texture, x, y, originX, originY, scaleX, scaleY, rotation, color, 0, 0, texture.Width, texture.Height);

        public static void Draw(this QuadRenderer renderer, Texture2dRegion texture, float x, float y, float originX, float originY, float scaleX, float scaleY, float rotation, Color4 color,
            float textureX0, float textureY0, float textureX1, float textureY1)
        {
            var width = textureX1 - textureX0;
            var height = textureY1 - textureY0;

            var fx = -originX;
            var fy = -originY;
            var fx2 = width - originX;
            var fy2 = height - originY;

            var flipX = false;
            var flipY = false;

            if (scaleX != 1 || scaleY != 1)
            {
                flipX = scaleX < 0;
                flipY = scaleY < 0;

                var absScaleX = flipX ? -scaleX : scaleX;
                var absScaleY = flipY ? -scaleY : scaleY;

                fx *= absScaleX;
                fy *= absScaleY;
                fx2 *= absScaleX;
                fy2 *= absScaleY;
            }

            var p1x = fx;
            var p1y = fy;
            var p2x = fx;
            var p2y = fy2;
            var p3x = fx2;
            var p3y = fy2;
            var p4x = fx2;
            var p4y = fy;

            float x1, y1, x2, y2, x3, y3, x4, y4;

            if (rotation != 0)
            {
                var cos = (float)Math.Cos(rotation);
                var sin = (float)Math.Sin(rotation);

                x1 = cos * p1x - sin * p1y;
                y1 = sin * p1x + cos * p1y;
                x2 = cos * p2x - sin * p2y;
                y2 = sin * p2x + cos * p2y;
                x3 = cos * p3x - sin * p3y;
                y3 = sin * p3x + cos * p3y;
                x4 = x1 + (x3 - x2);
                y4 = y3 - (y2 - y1);
            }
            else
            {
                x1 = p1x;
                y1 = p1y;
                x2 = p2x;
                y2 = p2y;
                x3 = p3x;
                y3 = p3y;
                x4 = p4x;
                y4 = p4y;
            }

            var primitive = default(QuadPrimitive);

            primitive.x1 = x1 + x;
            primitive.y1 = y1 + y;
            primitive.x2 = x2 + x;
            primitive.y2 = y2 + y;
            primitive.x3 = x3 + x;
            primitive.y3 = y3 + y;
            primitive.x4 = x4 + x;
            primitive.y4 = y4 + y;

            var textureUvBounds = texture.UvBounds;
            var textureUvRatio = texture.UvRatio;

            var textureU0 = textureUvBounds.Left + textureX0 * textureUvRatio.X;
            var textureV0 = textureUvBounds.Top + textureY0 * textureUvRatio.Y;
            var textureU1 = textureUvBounds.Left + textureX1 * textureUvRatio.X;
            var textureV1 = textureUvBounds.Top + textureY1 * textureUvRatio.Y;

            float u0, v0, u1, v1;
            if (flipX)
            {
                u0 = textureU1;
                u1 = textureU0;
            }
            else
            {
                u0 = textureU0;
                u1 = textureU1;
            }
            if (flipY)
            {
                v0 = textureV1;
                v1 = textureV0;
            }
            else
            {
                v0 = textureV0;
                v1 = textureV1;
            }

            primitive.u1 = u0;
            primitive.v1 = v0;
            primitive.u2 = u0;
            primitive.v2 = v1;
            primitive.u3 = u1;
            primitive.v3 = v1;
            primitive.u4 = u1;
            primitive.v4 = v0;

            primitive.color1 = primitive.color2 = primitive.color3 = primitive.color4 = color.ToRgba();
            renderer.Draw(ref primitive, texture);
        }

        public static void DrawArc(this QuadRenderer renderer, Vector2 center, float innerRadius, float outerRadius, float startAngle, float angleLength, Texture2dRegion texture, Color4 color, float precision = 1)
            => DrawArc(renderer, center, innerRadius, outerRadius, startAngle, angleLength, texture, color, color, precision);

        public static void DrawArc(this QuadRenderer renderer, Vector2 center, float innerRadius, float outerRadius, float startAngle, float angleLength, Texture2dRegion texture, Color4 innerColor, Color4 outerColor, float precision = 1)
        {
            texture = texture ?? DrawState.WhitePixel;
            var uMin = texture.UvBounds.Left;
            var uMax = texture.UvBounds.Right;
            var vMin = texture.UvBounds.Top;
            var vMax = texture.UvBounds.Bottom;

            var absAngleLength = Math.Abs(angleLength);
            var circumference = absAngleLength * outerRadius;
            var lineCount = Math.Max(2, Math.Max(absAngleLength / MathHelper.TwoPi * 8, (int)Math.Round(circumference * precision)));

            var u = (uMax - uMin) / lineCount;
            var angleStep = angleLength / lineCount;
            var innerColorRgba = innerColor.ToRgba();
            var outerColorRgba = outerColor.ToRgba();

            var initialUnit = new Vector2((float)Math.Cos(startAngle), (float)Math.Sin(startAngle));
            var primitive = new QuadPrimitive
            {
                x1 = center.X + initialUnit.X * outerRadius,
                y1 = center.Y + initialUnit.Y * outerRadius,
                x4 = center.X + initialUnit.X * innerRadius,
                y4 = center.Y + initialUnit.Y * innerRadius,

                u1 = uMin,
                u4 = uMin,
                v1 = vMin,
                v2 = vMin,
                v3 = vMax,
                v4 = vMax,

                color1 = outerColorRgba,
                color2 = outerColorRgba,
                color3 = innerColorRgba,
                color4 = innerColorRgba
            };

            for (var i = 1; i <= lineCount; i++)
            {
                var angle = startAngle + angleStep * i;

                var partU = uMin + u * i;
                primitive.u2 = partU;
                primitive.u3 = partU;

                var unit = new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle));
                primitive.x2 = center.X + unit.X * outerRadius;
                primitive.y2 = center.Y + unit.Y * outerRadius;
                primitive.x3 = center.X + unit.X * innerRadius;
                primitive.y3 = center.Y + unit.Y * innerRadius;

                renderer.Draw(ref primitive, texture);

                primitive.u1 = primitive.u2;
                primitive.u4 = primitive.u3;

                primitive.x1 = primitive.x2;
                primitive.y1 = primitive.y2;
                primitive.x4 = primitive.x3;
                primitive.y4 = primitive.y3;
            }
        }
    }
}