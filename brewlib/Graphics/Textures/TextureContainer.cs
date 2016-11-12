using System;

namespace BrewLib.Graphics.Textures
{
    public interface TextureContainer : IDisposable
    {
        Texture2d Get(string filename);
    }
}
