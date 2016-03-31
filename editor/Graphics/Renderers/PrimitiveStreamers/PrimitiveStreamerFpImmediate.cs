using OpenTK.Graphics.OpenGL;
using System;
using System.Diagnostics;

namespace StorybrewEditor.Graphics.Renderers.PrimitiveStreamers
{
    /// <summary>
    /// [requires: v1.0]
    /// </summary>
    public class PrimitiveStreamerFpImmediate<TPrimitive> : PrimitiveStreamer<TPrimitive> where TPrimitive : struct
    {
        protected Action<TPrimitive> renderAction;
        protected bool bound;

        public bool SupportsShaders => false;
        public int DiscardedBufferCount { get; protected set; }
        public int BufferWaitCount { get; protected set; }

        private PrimitiveStreamerFpImmediate(Action<TPrimitive> renderAction)
        {
            this.renderAction = renderAction;
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
        }

        public void Bind(Shader shader)
        {
            if (shader != null) throw new ArgumentException("shaders aren't supported");
            if (bound) throw new InvalidOperationException("Already bound");
            bound = true;
        }

        public void Unbind()
        {
            if (!bound) throw new InvalidOperationException("Not bound");
            bound = false;
        }

        public void Render(PrimitiveType primitiveType, TPrimitive[] primitives, int primitiveCount, int drawCount, bool canBuffer = false)
        {
            Debug.Assert(primitiveCount <= primitives.Length);
            Debug.Assert(primitiveCount % drawCount == 0);

            GL.Begin(primitiveType);
            for (var i = 0; i < primitiveCount; i++)
                renderAction(primitives[i]);
            GL.End();
        }

        public static PrimitiveStreamer<SpritePrimitive> CreateSpriteStreamer()
        {
            return new PrimitiveStreamerFpImmediate<SpritePrimitive>(primitive =>
            {
                // For now, assume that all vertices have the same color
                Debug.Assert(primitive.color1 == primitive.color2
                    && primitive.color2 == primitive.color3
                    && primitive.color3 == primitive.color4);

                var color = primitive.color1;
                GL.Color4((byte)(color & 0xFF),
                    (byte)((color >> 8) & 0xFF),
                    (byte)((color >> 16) & 0xFF),
                    (byte)((color >> 24) & 0xFF));

                GL.TexCoord2(primitive.u1, primitive.v1);
                GL.Vertex2(primitive.x1, primitive.y1);

                GL.TexCoord2(primitive.u2, primitive.v2);
                GL.Vertex2(primitive.x2, primitive.y2);

                GL.TexCoord2(primitive.u3, primitive.v3);
                GL.Vertex2(primitive.x3, primitive.y3);

                GL.TexCoord2(primitive.u4, primitive.v4);
                GL.Vertex2(primitive.x4, primitive.y4);
            });
        }
    }
}
