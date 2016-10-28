using System.Collections.Generic;
using System.Resources;

namespace BrewLib.Graphics.Textures
{
    public class TextureContainerSeparate : TextureContainer
    {
        private ResourceManager resourceManager;

        private Dictionary<string, Texture2d> textures = new Dictionary<string, Texture2d>();

        public TextureContainerSeparate(ResourceManager resourceManager = null)
        {
            this.resourceManager = resourceManager;
        }

        public void Dispose()
        {
            foreach (var textureEntry in textures)
                textureEntry.Value?.Dispose();
            textures.Clear();
        }

        public Texture2d Get(string filename, bool sRgb = false)
        {
            // todo: srgb isn't cached

            Texture2d texture;
            if (!textures.TryGetValue(filename, out texture))
            {
                texture = Texture2d.Load(filename, sRgb, resourceManager);
                textures.Add(filename, texture);
            }
            return texture;
        }
    }
}
