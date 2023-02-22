using OpenTK.Graphics.OpenGL;
using System;
using System.Diagnostics;

namespace BrewLib.Graphics.Renderers.PrimitiveStreamers
{
    public class PrimitiveStreamerBufferData<TPrimitive> : PrimitiveStreamerVao<TPrimitive> where TPrimitive : struct
    {
        public PrimitiveStreamerBufferData(VertexDeclaration vertexDeclaration, int minRenderableVertexCount, ushort[] indexes = null)
            : base(vertexDeclaration, minRenderableVertexCount, indexes) { }

        public override void Render(PrimitiveType primitiveType, TPrimitive[] primitives, int primitiveCount, int drawCount, bool canBuffer = false)
        {
            Debug.Assert(primitiveCount <= primitives.Length);
            Debug.Assert(drawCount % primitiveCount == 0);

            GL.BufferData(BufferTarget.ArrayBuffer, (IntPtr)(primitiveCount * PrimitiveSize), primitives, BufferUsageHint.StreamDraw);
            DiscardedBufferCount++;

            if (IndexBufferId != -1) GL.DrawElements(primitiveType, drawCount, DrawElementsType.UnsignedShort, 0);
            else GL.DrawArrays(primitiveType, 0, drawCount);
        }
        protected override void internalBind(Shader shader)
        {
            base.internalBind(shader);
            GL.BindBuffer(BufferTarget.ArrayBuffer, VertexBufferId);
        }
        public new static bool HasCapabilities() => DrawState.HasCapabilities(1, 5) && PrimitiveStreamerVao<TPrimitive>.HasCapabilities();
    }
}