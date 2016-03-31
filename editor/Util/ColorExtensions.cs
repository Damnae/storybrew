using OpenTK.Graphics;

namespace StorybrewEditor.Util
{
    public static class ColorExtensions
    {
        public static int ToRgba(this Color4 color)
        {
            return ((int)(color.A * 255) << 24) | ((int)(color.B * 255) << 16) | ((int)(color.G * 255) << 8) | (int)(color.R * 255);
        }

        public static Color4 ToColor4(this int color)
        {
            return new Color4(
                (byte)(color & 0xFF),
                (byte)((color >> 8) & 0xFF),
                (byte)((color >> 16) & 0xFF),
                (byte)(color >> 24));
        }

        public static Color4 Lerp(this Color4 color, Color4 otherColor, float blend)
        {
            var invBlend = 1 - blend;
            return new Color4(
                color.R * invBlend + otherColor.R * blend,
                color.G * invBlend + otherColor.G * blend,
                color.B * invBlend + otherColor.B * blend,
                color.A * invBlend + otherColor.A * blend);
        }

        public static Color4 WithOpacity(this Color4 color, float opacity) => new Color4(color.R, color.G, color.B, color.A * opacity);
    }
}
