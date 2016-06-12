using OpenTK.Graphics.OpenGL;
using System;
using System.Runtime.InteropServices;

namespace StorybrewEditor.Graphics.Renderers.PrimitiveStreamers
{
    /// <summary>
    /// [requires: v2.0]
    /// [requires: v3.0 or ARB_vertex_array_object|VERSION_3_0]
    /// </summary>
    public abstract class PrimitiveStreamerVao<TPrimitive> : PrimitiveStreamer<TPrimitive> where TPrimitive : struct
    {
        protected int minRenderableVertexCount;
        protected VertexDeclaration vertexDeclaration;
        protected int primitiveSize;

        protected int vertexArrayId = -1;
        protected int vertexBufferId = -1;
        protected int indexBufferId = -1;

        protected Shader currentShader;
        protected bool bound;
        
        public int DiscardedBufferCount { get; protected set; }
        public int BufferWaitCount { get; protected set; }

        public PrimitiveStreamerVao(VertexDeclaration vertexDeclaration, int minRenderableVertexCount, ushort[] indexes = null)
        {
            if (vertexDeclaration.AttributeCount < 1) throw new ArgumentException("At least one vertex attribute is required");
            if (indexes != null && minRenderableVertexCount > ushort.MaxValue) throw new ArgumentException("Can't have more than " + ushort.MaxValue + " indexed vertices");

            this.minRenderableVertexCount = minRenderableVertexCount;
            this.vertexDeclaration = vertexDeclaration;
            primitiveSize = Marshal.SizeOf(default(TPrimitive));

            initializeVertexBuffer();
            if (indexes != null) initializeIndexBuffer(indexes);
        }

        protected virtual void initializeVertexBuffer()
        {
            vertexBufferId = GL.GenBuffer();
        }

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

        private void dispose(bool disposing)
        {
            if (!disposing)
                return;

            if (bound)
                Unbind();

            internalDispose();
        }

        protected virtual void internalDispose()
        {
            if (vertexArrayId != -1)
            {
                GL.BindVertexArray(0);
                GL.DeleteVertexArray(vertexArrayId);
                vertexArrayId = -1;
            }

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

            currentShader = null;
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
            if (currentShader != shader)
                setupVertexArray(shader);

            GL.BindVertexArray(vertexArrayId);
        }

        protected virtual void internalUnbind()
        {
            GL.BindVertexArray(0);
        }

        private void setupVertexArray(Shader shader)
        {
            bool initial = currentShader == null;

            if (initial)
                vertexArrayId = GL.GenVertexArray();

            GL.BindVertexArray(vertexArrayId);

            // Vertex

            GL.BindBuffer(BufferTarget.ArrayBuffer, vertexBufferId);

            if (!initial) vertexDeclaration.DeactivateAttributes(currentShader);
            vertexDeclaration.ActivateAttributes(shader);

            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);

            // Index

            if (initial && indexBufferId != -1)
                GL.BindBuffer(BufferTarget.ElementArrayBuffer, indexBufferId);

            GL.BindVertexArray(0);

            currentShader = shader;
        }

        public abstract void Render(PrimitiveType primitiveType, TPrimitive[] primitives, int primitiveCount, int drawCount, bool canBuffer = false);

        public static bool HasCapabilities()
        {
            return DrawState.HasCapabilities(2, 0)
                && DrawState.HasCapabilities(3, 0, "GL_ARB_vertex_array_object");
        }
    }
}
