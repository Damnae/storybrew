using BrewLib.Graphics.Renderers;
using BrewLib.Graphics.Textures;
using System;
using System.Resources;

namespace BrewLib.Graphics
{
    public class DrawContext : IDisposable
    {
        public TextureContainer TextureContainer { get; private set; }
        public SpriteRenderer SpriteRenderer { get; private set; }

        public DrawContext(ResourceManager resourceManager = null)
        {
            TextureContainer = new TextureContainerSeparate(resourceManager);
            SpriteRenderer = new SpriteRendererBuffered();
        }

        public void Dispose()
        {
            SpriteRenderer.Dispose();
            SpriteRenderer = null;

            TextureContainer.Dispose();
            TextureContainer = null;
        }
    }
}
