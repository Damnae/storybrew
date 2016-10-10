using OpenTK;
using System;

namespace StorybrewEditor.Graphics.Textures
{
    public class Texture2dSlice : Texture
    {
        private string description;
        public string Description => texture != null ? $"{description} (from {texture.Description})" : description;

        private Texture2d texture;
        public BindableTexture BindableTexture => texture;

        private Box2 bounds;
        public float Width => bounds.Width;
        public float Height => bounds.Height;
        public Vector2 Size => new Vector2(bounds.Width, bounds.Height);
        public Box2 UvBounds => Box2.FromTLRB(bounds.Top / texture.Height, bounds.Left / texture.Width, bounds.Right / texture.Width, bounds.Bottom / texture.Height);
        public Vector2 UvRatio => new Vector2(1f / texture.Width, 1f / texture.Height);

        public Texture2dSlice(Texture2d texture, Box2 bounds, string description)
        {
            this.texture = texture ?? this as Texture2d;
            this.bounds = bounds;
            this.description = description;
        }

        public override string ToString()
            => $"Texture2dSlice#{texture.TextureId} {Description} ({Width}x{Height})";

        #region IDisposable Support

        private bool disposedValue = false;
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

        ~Texture2dSlice()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion
    }
}
