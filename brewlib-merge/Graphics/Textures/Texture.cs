using System;

namespace BrewLib.Graphics.Textures
{
    public interface Texture : IDisposable
    {
        string Description { get; }
        BindableTexture BindableTexture { get; }
    }
}