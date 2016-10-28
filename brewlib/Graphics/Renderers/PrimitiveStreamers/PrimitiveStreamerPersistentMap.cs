using BrewLib.Util;
using OpenTK.Graphics.OpenGL;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace BrewLib.Graphics.Renderers.PrimitiveStreamers
{
    /// <summary>
    /// [requires: v1.5]
    /// [requires: v3.0 or ARB_map_buffer_range|VERSION_3_0]
    /// [requires: v4.4 or ARB_buffer_storage|VERSION_4_4]
    /// [include requirements for GpuCommandSync]
    /// [include requirements for PrimitiveStreamerVao]
    /// </summary>
    public class PrimitiveStreamerPersistentMap<TPrimitive> : PrimitiveStreamerVao<TPrimitive>, PrimitiveStreamer<TPrimitive> where TPrimitive : struct
    {
        private GpuCommandSync commandSync;
        private int vertexBufferSize;

        private IntPtr bufferPointer;
        private int bufferOffset;
        private int drawOffset;

        public PrimitiveStreamerPersistentMap(VertexDeclaration vertexDeclaration, int minRenderableVertexCount, ushort[] indexes = null)
            : base(vertexDeclaration, minRenderableVertexCount, indexes)
        {
            commandSync = new GpuCommandSync();
        }

        protected override void initializeVertexBuffer()
        {
            base.initializeVertexBuffer();
            vertexBufferSize = minRenderableVertexCount * vertexDeclaration.VertexSize;

            GL.BindBuffer(BufferTarget.ArrayBuffer, vertexBufferId);

            GL.BufferStorage(BufferTarget.ArrayBuffer, (IntPtr)vertexBufferSize, IntPtr.Zero, BufferStorageFlags.MapWriteBit | BufferStorageFlags.MapPersistentBit | BufferStorageFlags.MapCoherentBit);
            bufferPointer = GL.MapBufferRange(BufferTarget.ArrayBuffer, IntPtr.Zero, (IntPtr)vertexBufferSize, BufferAccessMask.MapWriteBit | BufferAccessMask.MapPersistentBit | BufferAccessMask.MapCoherentBit);
            DrawState.CheckError("mapping vertex buffer", bufferPointer == null);

            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
        }

        protected override void internalDispose()
        {
            GL.BindBuffer(BufferTarget.ArrayBuffer, vertexBufferId);
            GL.UnmapBuffer(BufferTarget.ArrayBuffer);

            commandSync.Dispose();
            commandSync = null;

            base.internalDispose();
        }

        public override void Render(PrimitiveType primitiveType, TPrimitive[] primitives, int primitiveCount, int drawCount, bool canBuffer = false)
        {
            if (!bound) throw new InvalidOperationException("Not bound");

            Debug.Assert(primitiveCount <= primitives.Length);
            Debug.Assert(drawCount % primitiveCount == 0);

            var vertexDataSize = primitiveCount * primitiveSize;
            Debug.Assert(vertexDataSize <= vertexBufferSize);

            if (bufferOffset + vertexDataSize > vertexBufferSize)
            {
                bufferOffset = 0;
                drawOffset = 0;
            }

            if (commandSync.WaitForRange(bufferOffset, vertexDataSize))
            {
                BufferWaitCount++;
                expandVertexBuffer();
            }

            GCHandle pinnedVertexData = GCHandle.Alloc(primitives, GCHandleType.Pinned);
            try
            {
                Native.CopyMemory(bufferPointer + bufferOffset, pinnedVertexData.AddrOfPinnedObject(), (uint)vertexDataSize);
            }
            finally
            {
                pinnedVertexData.Free();
            }

            if (indexBufferId != -1)
                GL.DrawElements(primitiveType, drawCount, DrawElementsType.UnsignedShort, drawOffset * sizeof(ushort));
            else
                GL.DrawArrays(primitiveType, drawOffset, drawCount);

            commandSync.LockRange(bufferOffset, vertexDataSize);

            bufferOffset += vertexDataSize;
            drawOffset += drawCount;
        }

        private void expandVertexBuffer()
        {
            if (indexBufferId != -1)
                return;

            // Prevent the vertex buffer from becoming too large (maxes at 8mb * grow factor)
            if (minRenderableVertexCount * vertexDeclaration.VertexSize > 8 * 1024 * 1024)
                return;

            minRenderableVertexCount = (int)(minRenderableVertexCount * 1.75);

            if (commandSync.WaitForAll())
                BufferWaitCount++;

            Unbind();

            // Rebuild the VBO

            GL.BindBuffer(BufferTarget.ArrayBuffer, vertexBufferId);
            GL.UnmapBuffer(BufferTarget.ArrayBuffer);
            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
            GL.DeleteBuffer(vertexBufferId);

            initializeVertexBuffer();

            // Rebuild the VAO

            GL.BindVertexArray(0);
            GL.DeleteVertexArray(vertexArrayId);

            var previousShader = currentShader;
            currentShader = null;
            Bind(previousShader);

            Debug.WriteLine("Expanded the vertex buffer to " + minRenderableVertexCount + " vertices (" + (vertexBufferSize / 1024) + "kb)");

            bufferOffset = 0;
            drawOffset = 0;

            DiscardedBufferCount++;
        }

        public new static bool HasCapabilities()
        {
            return DrawState.HasCapabilities(4, 4, "GL_ARB_buffer_storage")
                && DrawState.HasCapabilities(3, 0, "GL_ARB_map_buffer_range")
                && DrawState.HasCapabilities(1, 5)
                && GpuCommandSync.HasCapabilities()
                && PrimitiveStreamerVao<TPrimitive>.HasCapabilities();
        }
    }
}
