using OpenTK;
using OpenTK.Graphics;
using System;
using System.Drawing;

namespace BrewLib.Graphics.Textures
{
    public class TextureAtlas2d : IDisposable
    {
        Texture2d texture;
        readonly TextureOptions textureOptions;
        readonly int padding;

        int currentX, currentY, nextY;

        public float FillRatio => (texture.Width * currentY + currentX * (nextY - currentY)) / (texture.Width * texture.Height);

        public TextureAtlas2d(int width, int height, string description, TextureOptions textureOptions = null, int padding = 0)
        {
            texture = Texture2d.Create(new Color4(0, 0, 0, 0), description, width, height, textureOptions);
            this.textureOptions = textureOptions;
            this.padding = padding;
        }

        public Texture2dRegion AddRegion(Bitmap bitmap, string description)
        {
            if (currentY + bitmap.Height > texture.Height) return null;
            if (currentX + bitmap.Width > texture.Width)
            {
                if (nextY + bitmap.Height > texture.Height) return null;
                currentX = 0;
                currentY = nextY;
            }

            texture.Update(bitmap, currentX, currentY, textureOptions);
            var region = new Texture2dRegion(texture, new Box2(currentX, currentY, currentX + bitmap.Width, currentY + bitmap.Height), description);

            currentX += bitmap.Width + padding;
            nextY = Math.Max(nextY, currentY + bitmap.Height + padding);

            return region;
        }

        #region IDisposable Support

        bool disposedValue = false;
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing) texture.Dispose();
                texture = null;
                disposedValue = true;
            }
        }
        public void Dispose() => Dispose(true);

        #endregion
    }
}