using System;
using System.Collections.Generic;

namespace BrewLib.Graphics.Textures
{
    public delegate void ResourceLoadedDelegate<T>(string filename, T resource);

    public interface TextureContainer : IDisposable
    {
        IEnumerable<string> ResourceNames { get; }
        event ResourceLoadedDelegate<Texture2dRegion> ResourceLoaded;

        Texture2dRegion Get(string filename);
    }
}