using System;

namespace BrewLib.Graphics.Renderers.PrimitiveStreamers
{
    public static class PrimitiveStreamerUtil<TPrimitive> where TPrimitive : struct
    {
        public static readonly CreatePrimitiveStreamerDelegate<TPrimitive> DefaultCreatePrimitiveStreamer = (vertexDeclaration, minRenderableVertexCount) =>
        {
            if (PrimitiveStreamerPersistentMap<TPrimitive>.HasCapabilities())
                return new PrimitiveStreamerPersistentMap<TPrimitive>(vertexDeclaration, minRenderableVertexCount);

            else if (PrimitiveStreamerBufferData<TPrimitive>.HasCapabilities())
                return new PrimitiveStreamerBufferData<TPrimitive>(vertexDeclaration, minRenderableVertexCount);

            else if (PrimitiveStreamerVbo<TPrimitive>.HasCapabilities())
                return new PrimitiveStreamerVbo<TPrimitive>(vertexDeclaration);

            throw new NotSupportedException();
        };
    }
}