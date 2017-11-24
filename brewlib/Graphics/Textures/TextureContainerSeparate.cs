using BrewLib.Data;
using BrewLib.Util;
using System.Collections.Generic;
using System.Linq;

namespace BrewLib.Graphics.Textures
{
    public class TextureContainerSeparate : TextureContainer
    {
        private ResourceContainer resourceContainer;
        private TextureOptions textureOptions;

        private Dictionary<string, Texture2d> textures = new Dictionary<string, Texture2d>();

        public IEnumerable<string> ResourceNames
            => textures.Where(e => e.Value != null).Select(e => e.Key);

        public TextureContainerSeparate(ResourceContainer resourceContainer = null, TextureOptions textureOptions = null)
        {
            this.resourceContainer = resourceContainer;
            this.textureOptions = textureOptions;
        }

        public Texture2dRegion Get(string filename)
        {
            filename = PathHelper.WithStandardSeparators(filename);
            if (!textures.TryGetValue(filename, out Texture2d texture))
            {
                texture = Texture2d.Load(filename, resourceContainer, textureOptions);
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
                    foreach (var entry in textures)
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
