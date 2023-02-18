using BrewLib.Data;
using BrewLib.Util;
using System.Collections.Generic;
using System.Linq;

namespace BrewLib.Graphics.Textures
{
    public class TextureContainerAtlas : TextureContainer
    {
        readonly ResourceContainer resourceContainer;
        readonly TextureOptions textureOptions;
        readonly int width, height, padding;
        readonly string description;

        Dictionary<string, Texture2dRegion> textures = new Dictionary<string, Texture2dRegion>();
        readonly Dictionary<TextureOptions, TextureMultiAtlas2d> atlases = new Dictionary<TextureOptions, TextureMultiAtlas2d>();

        public IEnumerable<string> ResourceNames => textures.Where(e => e.Value != null).Select(e => e.Key);
        public event ResourceLoadedDelegate<Texture2dRegion> ResourceLoaded;

        public TextureContainerAtlas(ResourceContainer resourceContainer = null, TextureOptions textureOptions = null, int width = 512, int height = 512, int padding = 0, string description = nameof(TextureContainerAtlas))
        {
            this.resourceContainer = resourceContainer;
            this.textureOptions = textureOptions;
            this.width = width;
            this.height = height;
            this.padding = padding;
            this.description = description;
        }

        public Texture2dRegion Get(string filename)
        {
            if (filename == null) return null;

            filename = PathHelper.WithStandardSeparators(filename);
            if (!textures.TryGetValue(filename, out Texture2dRegion texture))
            {
                var textureOptions = this.textureOptions ?? Texture2d.LoadTextureOptions(filename, resourceContainer) ?? TextureOptions.Default;
                if (!atlases.TryGetValue(textureOptions, out TextureMultiAtlas2d atlas))
                    atlases.Add(textureOptions, atlas = new TextureMultiAtlas2d(width, height, $"{description} (Option set {atlases.Count})", textureOptions, padding));

                using (var bitmap = Texture2d.LoadBitmap(filename, resourceContainer)) if (bitmap != null)
                        texture = atlas.AddRegion(bitmap, filename);

                textures.Add(filename, texture);
                ResourceLoaded?.Invoke(filename, texture);
            }
            return texture;
        }

        #region IDisposable Support

        bool disposedValue = false;
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    foreach (var entry in atlases) entry.Value?.Dispose();
                    textures.Clear();
                }
                textures = null;
                disposedValue = true;
            }
        }
        public void Dispose() => Dispose(true);

        #endregion
    }
}