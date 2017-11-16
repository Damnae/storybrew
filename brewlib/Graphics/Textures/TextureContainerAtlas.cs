using BrewLib.Data;
using System.Collections.Generic;
using System.Resources;

namespace BrewLib.Graphics.Textures
{
    public class TextureContainerAtlas : TextureContainer
    {
        private ResourceContainer resourceContainer;
        private TextureOptions textureOptions;
        private int width;
        private int height;
        private string description;

        private Dictionary<string, Texture2dRegion> textures = new Dictionary<string, Texture2dRegion>();
        private Dictionary<TextureOptions, TextureMultiAtlas2d> atlases = new Dictionary<TextureOptions, TextureMultiAtlas2d>();

        public TextureContainerAtlas(ResourceContainer resourceContainer = null, TextureOptions textureOptions = null, int width = 512, int height = 512, string description = nameof(TextureContainerAtlas))
        {
            this.resourceContainer = resourceContainer;
            this.textureOptions = textureOptions;
            this.width = width;
            this.height = height;
            this.description = description;
        }

        public Texture2dRegion Get(string filename)
        {
            if (!textures.TryGetValue(filename, out Texture2dRegion texture))
            {
                var textureOptions = this.textureOptions ?? Texture2d.LoadTextureOptions(filename, resourceContainer) ?? TextureOptions.Default;
                if (!atlases.TryGetValue(textureOptions, out TextureMultiAtlas2d atlas))
                    atlases.Add(textureOptions, atlas = new TextureMultiAtlas2d(width, height, $"{description} (Option set {atlases.Count})", textureOptions));

                using (var bitmap = Texture2d.LoadBitmap(filename, resourceContainer))
                {
                    if (bitmap != null)
                        texture = atlas.AddRegion(bitmap, filename);
                }
                textures.Add(filename, texture);
            }
            return texture;
        }

        #region IDisposable Support

        private bool disposedValue = false;
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    foreach (var entry in atlases)
                        entry.Value?.Dispose();
                    textures.Clear();
                }
                textures = null;
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
