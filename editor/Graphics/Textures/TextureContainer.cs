using System;

namespace StorybrewEditor.Graphics.Textures
{
    public interface TextureContainer : IDisposable
    {
        Texture2d Get(string filename, bool sRgb = false);
    }
}
