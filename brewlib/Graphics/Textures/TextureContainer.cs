using System;
using System.Collections.Generic;

namespace BrewLib.Graphics.Textures
{
    public interface TextureContainer : IDisposable
    {
        IEnumerable<string> ResourceNames { get; }
        Texture2dRegion Get(string filename);
    }
}
