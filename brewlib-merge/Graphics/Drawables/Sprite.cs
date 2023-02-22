using BrewLib.Graphics.Cameras;
using BrewLib.Graphics.Renderers;
using BrewLib.Graphics.Textures;
using BrewLib.Util;
using OpenTK;
using OpenTK.Graphics;
using System;

namespace BrewLib.Graphics.Drawables
{
    public class Sprite : Drawable
    {
        public Texture2dRegion Texture;
        public readonly RenderStates RenderStates = new RenderStates();
        public float Rotation;
        public Color4 Color = Color4.White;
        public ScaleMode ScaleMode = ScaleMode.None;

        public Vector2 MinSize => Vector2.Zero;
        public Vector2 PreferredSize => Texture?.Size ?? Vector2.Zero;

        public void Draw(DrawContext drawContext, Camera camera, Box2 bounds, float opacity)
        {
            if (Texture == null) return;

            var renderer = DrawState.Prepare(drawContext.Get<QuadRenderer>(), camera, RenderStates);
            var color = Color.WithOpacity(opacity);

            var textureX0 = 0f;
            var textureY0 = 0f;
            var textureX1 = Texture.Width;
            var textureY1 = Texture.Height;

            var scaleH = bounds.Width / Texture.Width;
            var scaleV = bounds.Height / Texture.Height;

            float scale;
            switch (ScaleMode)
            {
                case ScaleMode.Fill:
                    if (scaleH > scaleV)
                    {
                        scale = scaleH;
                        textureY0 = (Texture.Height - bounds.Height / scale) / 2;
                        textureY1 = Texture.Height - textureY0;
                    }
                    else
                    {
                        scale = scaleV;
                        textureX0 = (Texture.Width - bounds.Width / scale) / 2;
                        textureX1 = Texture.Width - textureX0;
                    }
                    break;
                case ScaleMode.Fit:
                case ScaleMode.RepeatFit: scale = Math.Min(scaleH, scaleV); break;
                default: scale = 1f; break;
            }
            switch (ScaleMode)
            {
                case ScaleMode.Repeat:
                case ScaleMode.RepeatFit:
                    for (var y = bounds.Top; y < bounds.Bottom; y += Texture.Height * scale)
                        for (var x = bounds.Left; x < bounds.Right; x += Texture.Width * scale)
                        {
                            var textureX = Math.Min((bounds.Right - x) / scale, Texture.Width);
                            var textureY = Math.Min((bounds.Bottom - y) / scale, Texture.Height);
                            renderer.Draw(Texture, x, y, 0, 0, scale, scale, 0, color, 0, 0, textureX, textureY);
                        }
                    break;

                default:
                    renderer.Draw(Texture, (bounds.Left + bounds.Right) / 2, (bounds.Top + bounds.Bottom) / 2,
                    (textureX1 - textureX0) / 2, (textureY1 - textureY0) / 2,
                    scale, scale, Rotation, color, textureX0, textureY0, textureX1, textureY1);
                    break;
            }
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