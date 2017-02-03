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
        private double[,] kernel;

        private int radius = 6;
        public int Radius
        {
            get { return radius; }
            set
            {
                if (radius == value) return;
                radius = value;
                kernel = null;
            }
        }

        private double power = 0;
        public double Power
        {
            get { return power; }
            set
            {
                if (power == value) return;
                power = value;
                kernel = null;
            }
        }

        public Color4 Color = new Color4(255, 255, 255, 100);

        public bool Overlay => false;
        public Vector2 Measure() => new Vector2(Radius * 2);

        public void Draw(Bitmap bitmap, Graphics textGraphics, Font font, StringFormat stringFormat, string text, float x, float y)
        {
            if (Radius < 1)
                return;

            using (var blurSource = new Bitmap(bitmap.Width, bitmap.Height, PixelFormat.Format32bppArgb))
            {
                using (var brush = new SolidBrush(System.Drawing.Color.White))
                using (var graphics = Graphics.FromImage(blurSource))
                {
                    graphics.TextRenderingHint = textGraphics.TextRenderingHint;
                    graphics.SmoothingMode = SmoothingMode.HighQuality;
                    graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    graphics.DrawString(text, font, brush, x, y, stringFormat);
                }

                if (kernel == null)
                {
                    var radius = Math.Min(Radius, 24);
                    var power = Power >= 1 ? Power : Radius * 0.5;
                    kernel = BitmapHelper.CalculateGaussianKernel(radius, power);
                }

                using (var blurredBitmap = BitmapHelper.ConvoluteAlpha(blurSource, kernel, System.Drawing.Color.FromArgb(Color.ToArgb())))
                    textGraphics.DrawImage(blurredBitmap.Bitmap, 0, 0);
            }
        }
    }
}
