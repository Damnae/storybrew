using StorybrewEditor.Graphics.Renderers;
using StorybrewEditor.Graphics.Textures;
using System;

namespace StorybrewEditor.Graphics
{
    public class DrawContext : IDisposable
    {
        public TextureContainer TextureContainer { get; private set; }
        public SpriteRenderer SpriteRenderer { get; private set; }

        public DrawContext()
        {
            TextureContainer = new TextureContainerSeparate();
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
