using OpenTK;
using OpenTK.Graphics;
using System.Drawing;

namespace StorybrewCommon.Subtitles
{
    public class FontBackground : FontEffect
    {
        public Color4 Color = new Color4(0, 0, 0, 255);

        public bool Overlay => false;
        public Vector2 Measure() => Vector2.Zero;

        public void Draw(Bitmap bitmap, Graphics textGraphics, Font font, StringFormat stringFormat, string text, float x, float y)
        {
            textGraphics.Clear(System.Drawing.Color.FromArgb(Color.ToArgb()));
        }
    }
}
