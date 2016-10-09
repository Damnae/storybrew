using OpenTK;

namespace StorybrewEditor.Graphics.Textures
{
    public class Texture2dSlice : Texture
    {
        private Texture2d texture;
        private Box2 bounds;

        public int TextureId => texture.TextureId;
        public TexturingModes TexturingMode => texture.TexturingMode;

        public string description;
        public string Description => $"{description} (from {texture.Description})";
        
        public int Width => (int)bounds.Width;
        public int Height => (int)bounds.Height;

        public Box2 UvBounds => Box2.FromTLRB(bounds.Top / texture.Height, bounds.Left / texture.Width, bounds.Right / texture.Width, bounds.Bottom / texture.Height);

        public Texture BindableTexture => texture;

        public Texture2dSlice(Texture2d texture, Box2 bounds, string description)
        {
            this.texture = texture;
            this.bounds = bounds;
            this.description = description;
        }

        #region IDisposable Support

        private bool disposedValue = false;
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // Do not dispose the texture, it is disposed by the atlas
                    //texture.Dispose();
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
