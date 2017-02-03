using OpenTK;
using OpenTK.Graphics;
using StorybrewCommon.Util;
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace StorybrewCommon.Subtitles
{
    public class FontGlow : FontEffect
    {
        public int Radius = 6;
        public double Power = 0;
        public Color4 Color = new Color4(255, 255, 255, 100);

        public bool Overlay => false;
        public Vector2 Measure() => new Vector2(Radius * 2);

        public void Draw(Bitmap bitmap, Graphics textGraphics, Font font, StringFormat stringFormat, string text, float x, float y)
        {
            if (Radius < 1)
                return;

            using (var blurSource = new Bitmap(bitmap.Width, bitmap.Height, PixelFormat.Format32bppArgb))
            {
                using (var brush = new SolidBrush(System.Drawing.Color.FromArgb(Color.ToArgb())))
                using (var graphics = Graphics.FromImage(blurSource))
                {
                    graphics.TextRenderingHint = textGraphics.TextRenderingHint;
                    graphics.SmoothingMode = SmoothingMode.HighQuality;
                    graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    graphics.Clear(System.Drawing.Color.FromArgb(1, (byte)(Color.R * 255), (byte)(Color.G * 255), (byte)(Color.B * 255)));
                    graphics.DrawString(text, font, brush, x, y, stringFormat);
                }

                var radius = Math.Min(Radius, 24);
                var power = Power >= 1 ? Power : Radius * 0.5;
                using (var blurredBitmap = BitmapHelper.Blur(blurSource, radius, power))
                    textGraphics.DrawImage(blurredBitmap.Bitmap, 0, 0);
            }
        }
    }
}
