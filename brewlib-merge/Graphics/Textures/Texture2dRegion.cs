using OpenTK;
using System;
using System.Drawing;

namespace BrewLib.Graphics.Textures
{
    public class Texture2dRegion : Texture
    {
        readonly string description;
        public string Description => texture != this ? $"{description} (from {texture.Description})" : description;

        Texture2d texture;
        public BindableTexture BindableTexture => texture;

        Box2 bounds;
        public Box2 Bounds => bounds;
        public float X => bounds.Left;
        public float Y => bounds.Top;
        public float Width => bounds.Width;
        public float Height => bounds.Height;
        public Vector2 Size => new Vector2(bounds.Width, bounds.Height);
        public Box2 UvBounds => Box2.FromTLRB(bounds.Top / texture.Height, bounds.Left / texture.Width, bounds.Right / texture.Width, bounds.Bottom / texture.Height);
        public Vector2 UvRatio => new Vector2(1f / texture.Width, 1f / texture.Height);

        public Texture2dRegion(Texture2d texture, Box2 bounds, string description)
        {
            this.texture = texture ?? this as Texture2d;
            this.bounds = bounds;
            this.description = description;
        }

        public virtual void Update(Bitmap bitmap, int x, int y, TextureOptions textureOptions)
        {
            if (texture == null) throw new InvalidOperationException();
            if (x < 0 || y < 0) throw new ArgumentOutOfRangeException();

            var updateX = (int)bounds.Left + x;
            var updateY = (int)bounds.Top + y;

            if (updateX + bitmap.Width > bounds.Right || updateY + bitmap.Height > bounds.Bottom) throw new ArgumentOutOfRangeException();

            texture.Update(bitmap, updateX, updateY, textureOptions);
        }

        public override string ToString() => $"Texture2dRegion#{texture.TextureId} {Description} ({Width}x{Height})";

        #region IDisposable Support

        bool disposedValue = false;
        public bool Disposed => disposedValue;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                }
                texture = null;
                disposedValue = true;
            }
        }
        ~Texture2dRegion() => Dispose(false);

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion
    }
}