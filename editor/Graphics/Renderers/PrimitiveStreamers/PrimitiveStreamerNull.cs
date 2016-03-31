using OpenTK.Graphics.OpenGL;
using System;

namespace StorybrewEditor.Graphics.Renderers.PrimitiveStreamers
{
    public class PrimitiveStreamerNull<TPrimitive> : PrimitiveStreamer<TPrimitive> where TPrimitive : struct
    {
        public bool SupportsShaders => true;
        public int DiscardedBufferCount { get; protected set; }
        public int BufferWaitCount { get; protected set; }

        public PrimitiveStreamerNull()
        {
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }

        public void Bind(Shader shader)
        {
        }

        public void Unbind()
        {
        }

        public void Render(PrimitiveType primitiveType, TPrimitive[] primitives, int primitiveCount, int drawCount, bool canBuffer = false)
        {
        }
    }
}
