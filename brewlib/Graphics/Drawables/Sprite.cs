using BrewLib.Graphics.Cameras;
using BrewLib.Graphics.Textures;
using BrewLib.Util;
using OpenTK;
using OpenTK.Graphics;
using System;

namespace BrewLib.Graphics.Drawables
{
    public class Sprite : Drawable
    {
        public Texture2d Texture;
        public readonly RenderStates RenderStates = new RenderStates();
        public float Rotation;
        public Color4 Color = Color4.White;
        public ScaleMode ScaleMode = ScaleMode.None;

        public Vector2 MinSize => Vector2.Zero;
        public Vector2 PreferredSize => Texture?.Size ?? Vector2.Zero;

        public void Draw(DrawContext drawContext, Camera camera, Box2 bounds, float opacity)
        {
            if (Texture == null) return;

            var renderer = DrawState.Prepare(drawContext.SpriteRenderer, camera, RenderStates);
            var color = Color.WithOpacity(opacity);

            float textureX0 = 0;
            float textureY0 = 0;
            float textureX1 = Texture.Width;
            float textureY1 = Texture.Height;

            var scaleH = bounds.Width / Texture.Width;
            var scaleV = bounds.Height / Texture.Height;

            float scale;
            switch (ScaleMode)
            {
                case ScaleMode.Fill:
                    if (scaleH > scaleV)
                    {
                        scale = scaleH;
                        textureY0 = (Texture.Height - bounds.Height / scale) * 0.5f;
                        textureY1 = Texture.Height - textureY0;
                    }
                    else
                    {
                        scale = scaleV;
                        textureX0 = (Texture.Width - bounds.Width / scale) * 0.5f;
                        textureX1 = Texture.Width - textureX0;
                    }
                    break;
                case ScaleMode.Fit:
                case ScaleMode.RepeatFit:
                    scale = Math.Min(scaleH, scaleV);
                    break;
                default:
                    scale = 1f;
                    break;
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
                            renderer.Draw(Texture, x, y, 0, 0,
                                scale, scale, 0, color, 0, 0, textureX, textureY);
                        }
                    break;
                default:
                    renderer.Draw(Texture, (bounds.Left + bounds.Right) * 0.5f, (bounds.Top + bounds.Bottom) * 0.5f,
                        (textureX1 - textureX0) * 0.5f, (textureY1 - textureY0) * 0.5f,
                        scale, scale, Rotation, color, textureX0, textureY0, textureX1, textureY1);
                    break;
            }
        }

        #region IDisposable Support

        private bool disposedValue = false;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                Texture = null;
                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }

        #endregion
    }
}
