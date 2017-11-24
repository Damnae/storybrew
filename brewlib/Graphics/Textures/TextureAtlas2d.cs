using OpenTK;
using OpenTK.Graphics;
using System;
using System.Drawing;

namespace BrewLib.Graphics.Textures
{
    public class TextureAtlas2d : IDisposable
    {
        private Texture2d texture;
        private int padding;
        private int currentX;
        private int currentY;
        private int nextY;

        public float FillRatio => (texture.Width * currentY + currentX * (nextY - currentY)) / (texture.Width * texture.Height);

        public TextureAtlas2d(Texture2d texture, int padding = 0)
        {
            this.texture = texture;
            this.padding = padding;
        }

        public TextureAtlas2d(int width, int height, string description, TextureOptions textureOptions = null, int padding = 0)
            : this(Texture2d.Create(new Color4(0, 0, 0, 0), description, width, height, textureOptions), padding)
        {
        }

        /// <summary>
        /// Adds a bitmap to the atlas, return the new region or null if there isn't enough space
        /// </summary>
        public Texture2dRegion AddRegion(Bitmap bitmap, string description)
        {
            if (currentY + bitmap.Height > texture.Height) return null;
            if (currentX + bitmap.Width > texture.Width)
            {
                if (nextY + bitmap.Height > texture.Height) return null;
                currentX = 0;
                currentY = nextY;
            }

            texture.Update(bitmap, currentX, currentY);
            var region = new Texture2dRegion(texture, new Box2(currentX, currentY, currentX + bitmap.Width, currentY + bitmap.Height), description);

            currentX += bitmap.Width + padding;
            nextY = Math.Max(nextY, currentY + bitmap.Height + padding);

            return region;
        }

        #region IDisposable Support

        private bool disposedValue = false;
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    texture.Dispose();
                }
                texture = null;
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
