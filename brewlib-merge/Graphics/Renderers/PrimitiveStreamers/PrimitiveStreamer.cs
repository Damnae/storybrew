using OpenTK.Graphics.OpenGL;
using System;

namespace BrewLib.Graphics.Renderers.PrimitiveStreamers
{
    public interface PrimitiveStreamer<TPrimitive> : IDisposable where TPrimitive : struct
    {
        int DiscardedBufferCount { get; }
        int BufferWaitCount { get; }

        void Bind(Shader shader);
        void Unbind();

        void Render(PrimitiveType primitiveType, TPrimitive[] primitives, int primitiveCount, int drawCount, bool canBuffer = false);
    }
    public delegate PrimitiveStreamer<TPrimitive> CreatePrimitiveStreamerDelegate<TPrimitive>(VertexDeclaration vertexDeclaration, int minRenderableVertexCount)
        where TPrimitive : struct;
}