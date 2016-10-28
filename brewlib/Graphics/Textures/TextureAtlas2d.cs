using OpenTK;
using OpenTK.Graphics;
using System;
using System.Drawing;

namespace BrewLib.Graphics.Textures
{
    public class TextureAtlas2d : IDisposable
    {
        private Texture2d texture;
        private int currentX;
        private int currentY;
        private int nextY;

        public float FillRatio => (texture.Width * currentY + currentX * (nextY - currentY)) / (texture.Width * texture.Height);

        public TextureAtlas2d(Texture2d texture)
        {
            this.texture = texture;
        }

        public TextureAtlas2d(int width, int height, string description)
            : this(Texture2d.Create(new Color4(0, 0, 0, 0), description, width, height))
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

            currentX += bitmap.Width;
            nextY = Math.Max(nextY, currentY + bitmap.Height);

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
