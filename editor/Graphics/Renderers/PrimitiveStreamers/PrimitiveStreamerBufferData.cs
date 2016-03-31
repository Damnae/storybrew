using OpenTK.Graphics.OpenGL;
using System;
using System.Diagnostics;

namespace StorybrewEditor.Graphics.Renderers.PrimitiveStreamers
{
    /// <summary>
    /// [requires: v1.5]
    /// [include requirements for PrimitiveStreamerVao]
    /// </summary>
    public class PrimitiveStreamerBufferData<TPrimitive> : PrimitiveStreamerVao<TPrimitive> where TPrimitive : struct
    {

        public PrimitiveStreamerBufferData(VertexDeclaration vertexDeclaration, int minRenderableVertexCount, ushort[] indexes = null)
            : base(vertexDeclaration, minRenderableVertexCount, indexes)
        {
        }

        public override void Render(PrimitiveType primitiveType, TPrimitive[] primitives, int primitiveCount, int drawCount, bool canBuffer = false)
        {
            Debug.Assert(primitiveCount <= primitives.Length);
            Debug.Assert(drawCount % primitiveCount == 0);

            GL.BufferData(BufferTarget.ArrayBuffer, (IntPtr)(primitiveCount * primitiveSize), primitives, BufferUsageHint.StreamDraw);
            DiscardedBufferCount++;

            if (indexBufferId != -1)
                GL.DrawElements(primitiveType, drawCount, DrawElementsType.UnsignedShort, 0);
            else
                GL.DrawArrays(primitiveType, 0, drawCount);
        }

        protected override void internalBind(Shader shader)
        {
            base.internalBind(shader);
            GL.BindBuffer(BufferTarget.ArrayBuffer, vertexBufferId);
        }

        public new static bool HasCapabilities()
        {
            return DrawState.HasCapabilities(1, 5)
                && PrimitiveStreamerVao<TPrimitive>.HasCapabilities();
        }
    }
}
