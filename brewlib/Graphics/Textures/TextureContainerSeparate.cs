using System.Collections.Generic;
using System.Resources;

namespace BrewLib.Graphics.Textures
{
    public class TextureContainerSeparate : TextureContainer
    {
        private ResourceManager resourceManager;
        private TextureOptions textureOptions;

        private Dictionary<string, Texture2d> textures = new Dictionary<string, Texture2d>();

        public TextureContainerSeparate(ResourceManager resourceManager = null, TextureOptions textureOptions = null)
        {
            this.resourceManager = resourceManager;
            this.textureOptions = textureOptions;
        }

        public void Dispose()
        {
            foreach (var textureEntry in textures)
                textureEntry.Value?.Dispose();
            textures.Clear();
        }

        public Texture2d Get(string filename)
        {
            Texture2d texture;
            if (!textures.TryGetValue(filename, out texture))
            {
                texture = Texture2d.Load(filename, resourceManager, textureOptions);
                textures.Add(filename, texture);
            }
            return texture;
        }
    }
}
