using BrewLib.Util;
using OpenTK;
using OpenTK.Graphics;
using System.Drawing;
using System.Drawing.Drawing2D;
using Tiny;

namespace StorybrewCommon.Subtitles
{
    public class FontGradient : FontEffect
    {
        public Vector2 Offset = new Vector2(0, 0);
        public Vector2 Size = new Vector2(0, 24);
        public Color4 Color = new Color4(255, 0, 0, 0);
        public WrapMode WrapMode = WrapMode.TileFlipXY;

        public bool Overlay => true;
        public Vector2 Measure() => Vector2.Zero;

        public void Draw(Bitmap bitmap, Graphics textGraphics, Font font, StringFormat stringFormat, string text, float x, float y)
        {
            var transparentColor = Color.WithOpacity(0);
            using (var brush = new LinearGradientBrush(
                new PointF(x + Offset.X, y + Offset.Y),
                new PointF(x + Offset.X + Size.X, y + Offset.Y + Size.Y),
                System.Drawing.Color.FromArgb(Color.ToArgb()),
                System.Drawing.Color.FromArgb(transparentColor.ToArgb()))
                { WrapMode = WrapMode, })
                textGraphics.DrawString(text, font, brush, x, y, stringFormat);
        }

        public bool Matches(TinyToken cachedEffectRoot)
            => cachedEffectRoot.Value<string>("Type") == GetType().FullName &&
                MathUtil.FloatEquals(cachedEffectRoot.Value<float>("OffsetX"), Offset.X, 0.00001f) &&
                MathUtil.FloatEquals(cachedEffectRoot.Value<float>("OffsetY"), Offset.Y, 0.00001f) &&
                MathUtil.FloatEquals(cachedEffectRoot.Value<float>("SizeX"), Size.X, 0.00001f) &&
                MathUtil.FloatEquals(cachedEffectRoot.Value<float>("SizeY"), Size.Y, 0.00001f) &&
                MathUtil.FloatEquals(cachedEffectRoot.Value<float>("ColorR"), Color.R, 0.00001f) &&
                MathUtil.FloatEquals(cachedEffectRoot.Value<float>("ColorG"), Color.G, 0.00001f) &&
                MathUtil.FloatEquals(cachedEffectRoot.Value<float>("ColorB"), Color.B, 0.00001f) &&
                MathUtil.FloatEquals(cachedEffectRoot.Value<float>("ColorA"), Color.A, 0.00001f) &&
                cachedEffectRoot.Value<WrapMode>("WrapMode") == WrapMode;

        public TinyObject ToTinyObject() => new TinyObject
        {
            { "Type", GetType().FullName },
            { "OffsetX", Offset.X },
            { "OffsetY", Offset.Y },
            { "SizeX", Size.X },
            { "SizeY", Size.Y },
            { "ColorR", Color.R },
            { "ColorG", Color.G },
            { "ColorB", Color.B },
            { "ColorA", Color.A },
            { "WrapMode", WrapMode },
        };
    }
}
