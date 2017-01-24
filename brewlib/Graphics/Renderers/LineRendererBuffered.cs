using BrewLib.Graphics.Cameras;
using BrewLib.Graphics.Renderers.PrimitiveStreamers;
using BrewLib.Graphics.Shaders;
using BrewLib.Graphics.Shaders.Snippets;
using BrewLib.Util;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace BrewLib.Graphics.Renderers
{
    public class LineRendererBuffered : LineRenderer
    {
        public const int VertexPerLine = 2;
        public const string CombinedMatrixUniformName = "u_combinedMatrix";

        public static readonly VertexDeclaration VertexDeclaration =
            new VertexDeclaration(VertexAttribute.CreatePosition3d(), VertexAttribute.CreateColor(true));

        #region Default Shader

        public static Shader CreateDefaultShader()
        {
            var sb = new ShaderBuilder(VertexDeclaration);

            var combinedMatrix = sb.AddUniform(CombinedMatrixUniformName, "mat4");
            var color = sb.AddVarying("vec4");

            sb.VertexShader = new Sequence(
                new Assign(color, sb.VertexDeclaration.GetAttribute(AttributeUsage.Color)),
                new Assign(sb.GlPosition, () => $"{combinedMatrix.Ref} * vec4({sb.VertexDeclaration.GetAttribute(AttributeUsage.Position).Name}, 1)")
            );
            sb.FragmentShader = new Sequence(
                new Assign(sb.GlFragColor, () => $"{color.Ref}")
            );

            return sb.Build();
        }

        #endregion

        private Shader shader;
        private bool ownsShader;
        public Shader Shader => ownsShader ? null : shader;

        private Action flushAction;
        public Action FlushAction
        {
            get { return flushAction; }
            set { flushAction = value; }
        }

        private PrimitiveStreamer<LinePrimitive> primitiveStreamer;
        private LinePrimitive[] lineArray;

        private Camera camera;
        public Camera Camera
        {
            get { return camera; }
            set
            {
                if (camera == value)
                    return;

                if (rendering) DrawState.FlushRenderer();
                camera = value;
            }
        }

        private Matrix4 transformMatrix = Matrix4.Identity;
        public Matrix4 TransformMatrix
        {
            get { return transformMatrix; }
            set
            {
                if (transformMatrix.Equals(value))
                    return;

                DrawState.FlushRenderer();
                transformMatrix = value;
            }
        }

        private int linesInBatch;
        private int maxLinesPerBatch;
        private bool rendering;

        private int currentLargestBatch;

        public int RenderedSpriteCount { get; private set; }
        public int FlushedBufferCount { get; private set; }
        public int DiscardedBufferCount => primitiveStreamer.DiscardedBufferCount;
        public int BufferWaitCount => primitiveStreamer.BufferWaitCount;
        public int LargestBatch { get; private set; }

        public LineRendererBuffered(Shader shader = null, Action flushAction = null, int maxSpritesPerBatch = 4096, int primitiveBufferSize = 0) :
            this((vertexDeclaration, minRenderableVertexCount) =>
            {
                if (PrimitiveStreamerPersistentMap<SpritePrimitive>.HasCapabilities())
                    return new PrimitiveStreamerPersistentMap<LinePrimitive>(vertexDeclaration, minRenderableVertexCount);
                else if (PrimitiveStreamerBufferData<SpritePrimitive>.HasCapabilities())
                    return new PrimitiveStreamerBufferData<LinePrimitive>(vertexDeclaration, minRenderableVertexCount);
                else if (PrimitiveStreamerVbo<SpritePrimitive>.HasCapabilities())
                    return new PrimitiveStreamerVbo<LinePrimitive>(vertexDeclaration);
                throw new NotSupportedException();

            }, shader, flushAction, maxSpritesPerBatch, primitiveBufferSize)
        {
        }

        public LineRendererBuffered(CreatePrimitiveStreamerDelegate<LinePrimitive> createPrimitiveStreamer, Shader shader = null, Action flushAction = null, int maxSpritesPerBatch = 4096, int primitiveBufferSize = 0)
        {
            if (shader == null)
            {
                shader = CreateDefaultShader();
                ownsShader = true;
            }

            this.maxLinesPerBatch = maxSpritesPerBatch;
            this.flushAction = flushAction;
            this.shader = shader;

            var primitiveBatchSize = Math.Max(maxSpritesPerBatch, primitiveBufferSize / (VertexPerLine * VertexDeclaration.VertexSize));
            primitiveStreamer = createPrimitiveStreamer(VertexDeclaration, primitiveBatchSize * VertexPerLine);

            lineArray = new LinePrimitive[maxSpritesPerBatch];
            Trace.WriteLine($"Initialized {nameof(LineRenderer)} using {primitiveStreamer.GetType().Name}");
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public void Dispose(bool disposing)
        {
            if (!disposing)
                return;

            if (rendering)
                EndRendering();

            lineArray = null;

            primitiveStreamer.Dispose();
            primitiveStreamer = null;

            if (ownsShader) shader.Dispose();
            shader = null;
        }

        public void BeginRendering()
        {
            if (rendering) throw new InvalidOperationException("Already rendering");

            shader.Begin();
            primitiveStreamer.Bind(shader);

            rendering = true;
        }

        public void EndRendering()
        {
            if (!rendering) throw new InvalidOperationException("Not rendering");

            primitiveStreamer.Unbind();
            shader.End();

            rendering = false;
        }

        private bool lastFlushWasBuffered = false;
        public void Flush(bool canBuffer = false)
        {
            if (linesInBatch == 0)
                return;

            // When the previous flush was bufferable, draw state should stay the same.
            if (!lastFlushWasBuffered)
            {
                var combinedMatrix = transformMatrix * Camera.ProjectionView;
                GL.UniformMatrix4(shader.GetUniformLocation(CombinedMatrixUniformName), false, ref combinedMatrix);

                flushAction?.Invoke();
            }

            primitiveStreamer.Render(PrimitiveType.Lines, lineArray, linesInBatch, linesInBatch * VertexPerLine, canBuffer);

            currentLargestBatch += linesInBatch;
            if (!canBuffer)
            {
                LargestBatch = Math.Max(LargestBatch, currentLargestBatch);
                currentLargestBatch = 0;
            }

            linesInBatch = 0;
            FlushedBufferCount++;

            lastFlushWasBuffered = canBuffer;
        }

        public void Draw(Vector3 start, Vector3 end, Color4 color)
        {
            if (!rendering) throw new InvalidOperationException("Not rendering");

            if (linesInBatch == maxLinesPerBatch)
                DrawState.FlushRenderer(true);

            var linePrimitive = default(LinePrimitive);

            linePrimitive.x1 = start.X;
            linePrimitive.y1 = start.Y;
            linePrimitive.z1 = start.Z;
            linePrimitive.x2 = end.X;
            linePrimitive.y2 = end.Y;
            linePrimitive.z2 = end.Z;

            linePrimitive.color1 = linePrimitive.color2 = color.ToRgba();

            lineArray[linesInBatch] = linePrimitive;

            RenderedSpriteCount++;
            linesInBatch++;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct LinePrimitive
    {
        public float x1, y1, z1; public int color1;
        public float x2, y2, z2; public int color2;
    }
}
