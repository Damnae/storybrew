using BrewLib.Util;
using OpenTK;
using OpenTK.Graphics;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace StorybrewCommon.Subtitles
{
    ///<summary> A font gradient effect. </summary>
    public class FontGradient : FontEffect
    {
        ///<summary> The gradient offset of the effect. </summary>
        public Vector2 Offset = new Vector2(0, 0);

        ///<summary> The relative size of the gradient. </summary>
        public Vector2 Size = new Vector2(0, 24);

        ///<summary> The color tinting of the gradient. </summary>
        public Color4 Color = new Color4(255, 0, 0, 0);

        ///<summary> Specifies how the gradient is tiled when it is smaller than the area being filled. </summary>
        public WrapMode WrapMode = WrapMode.TileFlipXY;

        ///<inheritdoc/>
        public bool Overlay => true;

        ///<inheritdoc/>
        public Vector2 Measure() => Vector2.Zero;

        ///<inheritdoc/>
        public void Draw(Bitmap bitmap, Graphics textGraphics, Font font, StringFormat stringFormat, string text, float x, float y)
        {
            var transparentColor = Color.WithOpacity(0);
            using (var brush = new LinearGradientBrush(new PointF(x + Offset.X, y + Offset.Y),
                new PointF(x + Offset.X + Size.X, y + Offset.Y + Size.Y),
                System.Drawing.Color.FromArgb(Color.ToArgb()), System.Drawing.Color.FromArgb(transparentColor.ToArgb()))
            { WrapMode = WrapMode })
                textGraphics.DrawString(text, font, brush, x, y, stringFormat);
        }
    }
}