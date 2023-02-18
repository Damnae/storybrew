using BrewLib.Graphics.Textures;
using OpenTK;
using OpenTK.Graphics;
using System;

namespace BrewLib.Graphics.Renderers
{
    public interface SpriteRenderer : Renderer, IDisposable
    {
        Shader Shader { get; }
        Matrix4 TransformMatrix { get; set; }

        int RenderedSpriteCount { get; }
        int FlushedBufferCount { get; }
        int DiscardedBufferCount { get; }
        int BufferWaitCount { get; }
        int LargestBatch { get; }

        void Draw(Texture2dRegion texture, float x, float y, float originX, float originY, float scaleX, float scaleY, float rotation, Color4 color);
        void Draw(Texture2dRegion texture, float x, float y, float originX, float originY, float scaleX, float scaleY, float rotation, Color4 color, float textureX0, float textureY0, float textureX1, float textureY1);
    }
}
