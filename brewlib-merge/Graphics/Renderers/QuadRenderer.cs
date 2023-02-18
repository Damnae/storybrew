using BrewLib.Graphics.Textures;
using OpenTK;
using System;

namespace BrewLib.Graphics.Renderers
{
    public interface QuadRenderer : Renderer, IDisposable
    {
        Shader Shader { get; }
        Matrix4 TransformMatrix { get; set; }

        int RenderedQuadCount { get; }
        int FlushedBufferCount { get; }
        int DiscardedBufferCount { get; }
        int BufferWaitCount { get; }
        int LargestBatch { get; }

        void Draw(ref QuadPrimitive quad, Texture2dRegion texture);
    }
}