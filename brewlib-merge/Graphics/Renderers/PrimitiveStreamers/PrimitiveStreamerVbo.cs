using OpenTK.Graphics.OpenGL;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace BrewLib.Graphics.Renderers.PrimitiveStreamers
{
    /// <summary>
    /// [requires: v2.0]
    /// </summary>
    public class PrimitiveStreamerVbo<TPrimitive> : PrimitiveStreamer<TPrimitive> where TPrimitive : struct
    {
        readonly VertexDeclaration vertexDeclaration;
        readonly int primitiveSize;

        int vertexBufferId = -1;
        int indexBufferId = -1;

        Shader currentShader;
        bool bound;

        public int DiscardedBufferCount { get; protected set; }
        public int BufferWaitCount { get; protected set; }

        public PrimitiveStreamerVbo(VertexDeclaration vertexDeclaration, ushort[] indexes = null)
        {
            if (vertexDeclaration.AttributeCount < 1) throw new ArgumentException("At least one vertex attribute is required");

            this.vertexDeclaration = vertexDeclaration;
            primitiveSize = Marshal.SizeOf(default(TPrimitive));

            initializeVertexBuffer();
            if (indexes != null) initializeIndexBuffer(indexes);
        }

        protected virtual void initializeVertexBuffer() => vertexBufferId = GL.GenBuffer();
        protected virtual void initializeIndexBuffer(ushort[] indexes)
        {
            indexBufferId = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, indexBufferId);
            GL.BufferData(BufferTarget.ElementArrayBuffer, (IntPtr)(indexes.Length * sizeof(ushort)), indexes, BufferUsageHint.StaticDraw);
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, 0);
        }
        public void Dispose()
        {
            dispose(true);
            GC.SuppressFinalize(this);
        }
        void dispose(bool disposing)
        {
            if (!disposing) return;
            if (bound) Unbind();

            internalDispose();
        }
        protected virtual void internalDispose()
        {
            if (vertexBufferId != -1)
            {
                GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
                GL.DeleteBuffer(vertexBufferId);
                vertexBufferId = -1;
            }
            if (indexBufferId != -1)
            {
                GL.BindBuffer(BufferTarget.ElementArrayBuffer, 0);
                GL.DeleteBuffer(indexBufferId);
                indexBufferId = -1;
            }
        }
        public void Bind(Shader shader)
        {
            if (shader == null) throw new ArgumentNullException(nameof(shader));
            if (bound) throw new InvalidOperationException("Already bound");

            internalBind(shader);
            bound = true;
        }
        public void Unbind()
        {
            if (!bound) throw new InvalidOperationException("Not bound");

            internalUnbind();
            bound = false;
        }
        protected virtual void internalBind(Shader shader)
        {
            GL.BindBuffer(BufferTarget.ArrayBuffer, vertexBufferId);
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, indexBufferId);

            vertexDeclaration.ActivateAttributes(shader);
            currentShader = shader;
        }
        protected virtual void internalUnbind()
        {
            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, 0);

            vertexDeclaration.DeactivateAttributes(currentShader);
            currentShader = null;
        }
        public void Render(PrimitiveType primitiveType, TPrimitive[] primitives, int primitiveCount, int drawCount, bool canBuffer = false)
        {
            Debug.Assert(primitiveCount <= primitives.Length);
            Debug.Assert(drawCount % primitiveCount == 0);

            GL.BufferData(BufferTarget.ArrayBuffer, (IntPtr)(primitiveCount * primitiveSize), primitives, BufferUsageHint.StreamDraw);
            DiscardedBufferCount++;

            if (indexBufferId != -1) GL.DrawElements(primitiveType, drawCount, DrawElementsType.UnsignedShort, 0);
            else GL.DrawArrays(primitiveType, 0, drawCount);
        }
        public static bool HasCapabilities() => DrawState.HasCapabilities(2, 0);
    }
}