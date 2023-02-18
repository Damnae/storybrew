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

        public Action FlushAction;

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
            sb.FragmentShader = new Sequence(new Assign(sb.GlFragColor, () => $"{color.Ref}"));

            return sb.Build();
        }

        #endregion

        Shader shader;
        readonly int combinedMatrixLocation;

        public Shader Shader => ownsShader ? null : shader;
        readonly bool ownsShader;

        PrimitiveStreamer<LinePrimitive> primitiveStreamer;
        LinePrimitive[] primitives;

        Camera camera;
        public Camera Camera
        {
            get => camera;
            set
            {
                if (camera == value)
                    return;

                if (rendering) DrawState.FlushRenderer();
                camera = value;
            }
        }

        Matrix4 transformMatrix = Matrix4.Identity;
        public Matrix4 TransformMatrix
        {
            get => transformMatrix;
            set
            {
                if (transformMatrix.Equals(value))
                    return;

                DrawState.FlushRenderer();
                transformMatrix = value;
            }
        }

        int linesInBatch;
        readonly int maxLinesPerBatch;
        bool rendering;

        int currentLargestBatch;

        public int RenderedLineCount { get; set; }
        public int FlushedBufferCount { get; set; }
        public int DiscardedBufferCount => primitiveStreamer.DiscardedBufferCount;
        public int BufferWaitCount => primitiveStreamer.BufferWaitCount;
        public int LargestBatch { get; set; }

        public LineRendererBuffered(Shader shader = null, int maxLinesPerBatch = 4096, int primitiveBufferSize = 0)
            : this((vertexDeclaration, minRenderableVertexCount) =>
        {
            if (PrimitiveStreamerPersistentMap<LinePrimitive>.HasCapabilities())
                return new PrimitiveStreamerPersistentMap<LinePrimitive>(vertexDeclaration, minRenderableVertexCount);

            else if (PrimitiveStreamerBufferData<LinePrimitive>.HasCapabilities())
                return new PrimitiveStreamerBufferData<LinePrimitive>(vertexDeclaration, minRenderableVertexCount);

            else if (PrimitiveStreamerVbo<LinePrimitive>.HasCapabilities())
                return new PrimitiveStreamerVbo<LinePrimitive>(vertexDeclaration);

            throw new NotSupportedException();

        }, shader, maxLinesPerBatch, primitiveBufferSize)
        { }

        public LineRendererBuffered(CreatePrimitiveStreamerDelegate<LinePrimitive> createPrimitiveStreamer, Shader shader = null, int maxLinesPerBatch = 4096, int primitiveBufferSize = 0)
        {
            if (shader == null)
            {
                shader = CreateDefaultShader();
                ownsShader = true;
            }

            this.maxLinesPerBatch = maxLinesPerBatch;
            this.shader = shader;

            combinedMatrixLocation = shader.GetUniformLocation(CombinedMatrixUniformName);

            var primitiveBatchSize = Math.Max(maxLinesPerBatch, primitiveBufferSize / (VertexPerLine * VertexDeclaration.VertexSize));
            primitiveStreamer = createPrimitiveStreamer(VertexDeclaration, primitiveBatchSize * VertexPerLine);

            primitives = new LinePrimitive[maxLinesPerBatch];
            Trace.WriteLine($"Initialized {nameof(LineRenderer)} using {primitiveStreamer.GetType().Name}");
        }
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        public void Dispose(bool disposing)
        {
            if (!disposing) return;
            if (rendering) EndRendering();

            primitives = null;

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

        bool lastFlushWasBuffered = false;
        public void Flush(bool canBuffer = false)
        {
            if (linesInBatch == 0) return;

            // When the previous flush was bufferable, draw state should stay the same.
            if (!lastFlushWasBuffered)
            {
                var combinedMatrix = transformMatrix * Camera.ProjectionView;
                GL.UniformMatrix4(combinedMatrixLocation, false, ref combinedMatrix);

                FlushAction?.Invoke();
            }

            primitiveStreamer.Render(PrimitiveType.Lines, primitives, linesInBatch, linesInBatch * VertexPerLine, canBuffer);

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

        public void Draw(Vector3 start, Vector3 end, Color4 color) => Draw(start, end, color, color);
        public void Draw(Vector3 start, Vector3 end, Color4 startColor, Color4 endColor)
        {
            if (!rendering) throw new InvalidOperationException("Not rendering");
            if (linesInBatch == maxLinesPerBatch) DrawState.FlushRenderer(true);

            var linePrimitive = default(LinePrimitive);

            linePrimitive.x1 = start.X;
            linePrimitive.y1 = start.Y;
            linePrimitive.z1 = start.Z;
            linePrimitive.color1 = startColor.ToRgba();

            linePrimitive.x2 = end.X;
            linePrimitive.y2 = end.Y;
            linePrimitive.z2 = end.Z;
            linePrimitive.color2 = endColor.ToRgba();

            primitives[linesInBatch] = linePrimitive;

            RenderedLineCount++;
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