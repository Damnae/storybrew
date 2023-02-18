using BrewLib.Graphics.Cameras;
using BrewLib.Graphics.Renderers;
using BrewLib.Graphics.Textures;
using BrewLib.Util;
using OpenTK;
using OpenTK.Graphics;

namespace BrewLib.Graphics.Drawables
{
    public class NinePatch : Drawable
    {
        public Texture2dRegion Texture;
        public readonly RenderStates RenderStates = new RenderStates();
        public Color4 Color = Color4.White;
        public FourSide Borders;
        public FourSide Outset;
        public bool BordersOnly;

        public Vector2 MinSize => Texture != null ? new Vector2(
            Borders.Left + Texture.Width - Borders.Right - Outset.Horizontal,
            Borders.Top + Texture.Height - Borders.Bottom - Outset.Vertical) :
            Vector2.Zero;

        public Vector2 PreferredSize => MinSize;

        public void Draw(DrawContext drawContext, Camera camera, Box2 bounds, float opacity)
        {
            if (Texture == null) return;

            var x0 = bounds.Left - Outset.Left;
            var y0 = bounds.Top - Outset.Top;
            var x1 = x0 + Borders.Left;
            var y1 = y0 + Borders.Top;
            var x2 = bounds.Right + Outset.Right - (Texture.Width - Borders.Right);
            var y2 = bounds.Bottom + Outset.Bottom - (Texture.Height - Borders.Bottom);

            var horizontalScale = (x2 - x1) / (Borders.Right - Borders.Left);
            var verticalScale = (y2 - y1) / (Borders.Bottom - Borders.Top);

            var color = Color.WithOpacity(opacity);
            var renderer = DrawState.Prepare(drawContext.Get<QuadRenderer>(), camera, RenderStates);

            // Center
            if (!BordersOnly && horizontalScale > 0 && verticalScale > 0) renderer.Draw(Texture, x1, y1, 0, 0,
                horizontalScale, verticalScale, 0, color, Borders.Left, Borders.Top, Borders.Right, Borders.Bottom);

            // Sides
            if (verticalScale > 0)
            {
                renderer.Draw(Texture, x0, y1, 0, 0, 1, verticalScale, 0, color, 0, Borders.Top, Borders.Left, Borders.Bottom);
                renderer.Draw(Texture, x2, y1, 0, 0, 1, verticalScale, 0, color, Borders.Right, Borders.Top, Texture.Width, Borders.Bottom);
            }
            if (horizontalScale > 0)
            {
                renderer.Draw(Texture, x1, y0, 0, 0, horizontalScale, 1, 0, color, Borders.Left, 0, Borders.Right, Borders.Top);
                renderer.Draw(Texture, x1, y2, 0, 0, horizontalScale, 1, 0, color, Borders.Left, Borders.Bottom, Borders.Right, Texture.Height);
            }

            // Corners
            renderer.Draw(Texture, x0, y0, 0, 0, 1, 1, 0, color, 0, 0, Borders.Left, Borders.Top);
            renderer.Draw(Texture, x2, y0, 0, 0, 1, 1, 0, color, Borders.Right, 0, Texture.Width, Borders.Top);
            renderer.Draw(Texture, x0, y2, 0, 0, 1, 1, 0, color, 0, Borders.Bottom, Borders.Left, Texture.Height);
            renderer.Draw(Texture, x2, y2, 0, 0, 1, 1, 0, color, Borders.Right, Borders.Bottom, Texture.Width, Texture.Height);
        }

        #region IDisposable Support

        protected virtual void Dispose(bool disposing)
        {
            if (disposing) { }
            Texture = null;
        }
        public void Dispose() => Dispose(true);

        #endregion
    }
}