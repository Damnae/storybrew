using System;

namespace BrewLib.Graphics.Textures
{
    public interface TextureContainer : IDisposable
    {
        Texture2dRegion Get(string filename);
    }
}
