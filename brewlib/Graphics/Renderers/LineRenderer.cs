using OpenTK;
using OpenTK.Graphics;
using System;

namespace BrewLib.Graphics.Renderers
{
    public interface LineRenderer : Renderer, IDisposable
    {
        Shader Shader { get; }
        Matrix4 TransformMatrix { get; set; }

        int RenderedSpriteCount { get; }
        int FlushedBufferCount { get; }
        int DiscardedBufferCount { get; }
        int BufferWaitCount { get; }
        int LargestBatch { get; }

        void Draw(Vector3 start, Vector3 end, Color4 color);
    }
}
