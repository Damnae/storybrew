using OpenTK;
using OpenTK.Graphics;
using System.Drawing;

namespace StorybrewCommon.Subtitles
{
    ///<summary> A font background effect. </summary>
    public class FontBackground : FontEffect
    {
        ///<summary> The coloring tint of the glow. </summary>
        public Color4 Color = new Color4(0, 0, 0, 255);

        ///<inheritdoc/>
        public bool Overlay => false;

        ///<inheritdoc/>
        public Vector2 Measure() => Vector2.Zero;

        ///<inheritdoc/>
        public void Draw(Bitmap bitmap, Graphics textGraphics, Font font, StringFormat stringFormat, string text, float x, float y)
            => textGraphics.Clear(System.Drawing.Color.FromArgb(Color.ToArgb()));
    }
}