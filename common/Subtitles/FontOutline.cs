using OpenTK;
using OpenTK.Graphics;
using System.Drawing;

namespace StorybrewCommon.Subtitles
{
    public class FontOutline : FontEffect
    {
        private const float diagonal = 1.41421356237f;

        public int Thickness = 1;
        public Color4 Color = new Color4(0, 0, 0, 100);

        public bool Overlay => false;
        public Vector2 Measure() => new Vector2(Thickness * diagonal * 2);

        public void Draw(Bitmap bitmap, Graphics textGraphics, Font font, StringFormat stringFormat, string text, float x, float y)
        {
            if (Thickness < 1)
                return;

            using (var brush = new SolidBrush(System.Drawing.Color.FromArgb(Color.ToArgb())))
                for (var i = 1; i <= Thickness; i++)
                    if (i % 2 == 0)
                    {
                        textGraphics.DrawString(text, font, brush, x - i * diagonal, y, stringFormat);
                        textGraphics.DrawString(text, font, brush, x, y - i * diagonal, stringFormat);
                        textGraphics.DrawString(text, font, brush, x + i * diagonal, y, stringFormat);
                        textGraphics.DrawString(text, font, brush, x, y + i * diagonal, stringFormat);
                    }
                    else
                    {
                        textGraphics.DrawString(text, font, brush, x - i, y - i, stringFormat);
                        textGraphics.DrawString(text, font, brush, x - i, y + i, stringFormat);
                        textGraphics.DrawString(text, font, brush, x + i, y + i, stringFormat);
                        textGraphics.DrawString(text, font, brush, x + i, y - i, stringFormat);
                    }
        }
    }
}
