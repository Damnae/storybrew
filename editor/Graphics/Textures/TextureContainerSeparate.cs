using System.Collections.Generic;

namespace StorybrewEditor.Graphics.Textures
{
    public class TextureContainerSeparate : TextureContainer
    {
        private bool allowResources;

        private Dictionary<string, Texture2d> textures = new Dictionary<string, Texture2d>();

        public TextureContainerSeparate(bool allowResources = true)
        {
            this.allowResources = allowResources;
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
                texture = Texture2d.Load(filename, sRgb, allowResources);
                textures.Add(filename, texture);
            }
            return texture;
        }
    }
}
