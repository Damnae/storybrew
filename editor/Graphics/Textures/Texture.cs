using System;

namespace StorybrewEditor.Graphics.Textures
{
    public interface Texture : IDisposable
    {
        string Description { get; }
        BindableTexture BindableTexture { get; }
    }
}
