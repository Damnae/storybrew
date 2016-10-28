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

        /// <summary>
        /// Renders primitives.
        /// </summary>
        /// <param name="primitiveType">Type of primitive to draw</param>
        /// <param name="primitives">Array of primitives</param>
        /// <param name="primitiveCount">Amount of primitive to use from the primitives array</param>
        /// <param name="drawCount">Either the count of indexes or vertices to draw, depending if the primitives are indexed or not</param>
        /// <param name="canBuffer">Whether the drawing can be buffered until the next render call or the end call</param>
        void Render(PrimitiveType primitiveType, TPrimitive[] primitives, int primitiveCount, int drawCount, bool canBuffer = false);
    }

    public delegate PrimitiveStreamer<TPrimitive> CreatePrimitiveStreamerDelegate<TPrimitive>(VertexDeclaration vertexDeclaration, int minRenderableVertexCount) where TPrimitive : struct;
}
