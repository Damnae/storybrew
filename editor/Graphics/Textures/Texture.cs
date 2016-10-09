
using OpenTK;
using System;

namespace StorybrewEditor.Graphics.Textures
{
    public interface Texture : IDisposable
    {
        int TextureId { get; }
        TexturingModes TexturingMode { get; }
        string Description { get; }

        int Width { get; }
        int Height { get; }

        Box2 UvBounds { get; }

        Texture BindableTexture { get; }
    }
}
