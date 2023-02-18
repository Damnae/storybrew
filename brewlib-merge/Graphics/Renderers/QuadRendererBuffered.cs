using BrewLib.Graphics.Cameras;
using BrewLib.Graphics.Renderers.PrimitiveStreamers;
using BrewLib.Graphics.Shaders;
using BrewLib.Graphics.Shaders.Snippets;
using BrewLib.Graphics.Textures;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using System;
using System.Diagnostics;

namespace BrewLib.Graphics.Renderers
{
    public class QuadRendererBuffered : QuadRenderer
    {
        public const int VertexPerQuad = 4;
        public const string CombinedMatrixUniformName = "u_combinedMatrix";
        public const string TextureUniformName = "u_texture";

        public static readonly VertexDeclaration VertexDeclaration =
            new VertexDeclaration(VertexAttribute.CreatePosition2d(), VertexAttribute.CreateDiffuseCoord(0), VertexAttribute.CreateColor(true));

        public delegate int CustomTextureBinder(BindableTexture texture);
        public CustomTextureBinder CustomTextureBind;

        public Action FlushAction;

        #region Default Shader

        public static Shader CreateDefaultShader()
        {
            var sb = new ShaderBuilder(VertexDeclaration);

            var combinedMatrix = sb.AddUniform(CombinedMatrixUniformName, "mat4");
            var texture = sb.AddUniform(TextureUniformName, "sampler2D");

            var color = sb.AddVarying("vec4");
            var textureCoord = sb.AddVarying("vec2");

            sb.VertexShader = new Sequence(
                new Assign(color, sb.VertexDeclaration.GetAttribute(AttributeUsage.Color)),
                new Assign(textureCoord, sb.VertexDeclaration.GetAttribute(AttributeUsage.DiffuseMapCoord)),
                new Assign(sb.GlPosition, () => $"{combinedMatrix.Ref} * vec4({sb.VertexDeclaration.GetAttribute(AttributeUsage.Position).Name}, 0, 1)")
            );
            sb.FragmentShader = new Sequence(new Assign(sb.GlFragColor, () => $"{color.Ref} * texture2D({texture.Ref}, {textureCoord.Ref})"));

            return sb.Build();
        }

        #endregion

        Shader shader;
        readonly int combinedMatrixLocation;
        readonly int textureUniformLocation;

        public Shader Shader => ownsShader ? null : shader;
        readonly bool ownsShader;

        PrimitiveStreamer<QuadPrimitive> primitiveStreamer;
        QuadPrimitive[] primitives;

        Camera camera;
        public Camera Camera
        {
            get => camera;
            set
            {
                if (camera == value) return;

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
                if (transformMatrix.Equals(value)) return;

                DrawState.FlushRenderer();
                transformMatrix = value;
            }
        }

        int quadsInBatch;
        readonly int maxQuadsPerBatch;

        BindableTexture currentTexture;
        int currentSamplerUnit;
        bool rendering;

        int currentLargestBatch;

        public int RenderedQuadCount { get; set; }
        public int FlushedBufferCount { get; set; }
        public int DiscardedBufferCount => primitiveStreamer.DiscardedBufferCount;
        public int BufferWaitCount => primitiveStreamer.BufferWaitCount;
        public int LargestBatch { get; set; }

        public QuadRendererBuffered(Shader shader = null, int maxQuadsPerBatch = 4096, int primitiveBufferSize = 0)
            : this(PrimitiveStreamerUtil<QuadPrimitive>.DefaultCreatePrimitiveStreamer, shader, maxQuadsPerBatch, primitiveBufferSize) { }

        public QuadRendererBuffered(CreatePrimitiveStreamerDelegate<QuadPrimitive> createPrimitiveStreamer, Shader shader = null, int maxQuadsPerBatch = 4096, int primitiveBufferSize = 0)
        {
            if (shader == null)
            {
                shader = CreateDefaultShader();
                ownsShader = true;
            }

            this.maxQuadsPerBatch = maxQuadsPerBatch;
            this.shader = shader;

            combinedMatrixLocation = shader.GetUniformLocation(CombinedMatrixUniformName);
            textureUniformLocation = shader.GetUniformLocation(TextureUniformName);

            var primitiveBatchSize = Math.Max(maxQuadsPerBatch, primitiveBufferSize / (VertexPerQuad * VertexDeclaration.VertexSize));
            primitiveStreamer = createPrimitiveStreamer(VertexDeclaration, primitiveBatchSize * VertexPerQuad);

            primitives = new QuadPrimitive[maxQuadsPerBatch];
            Trace.WriteLine($"Initialized {nameof(QuadRenderer)} using {primitiveStreamer.GetType().Name}");
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

            currentTexture = null;
            rendering = false;
        }

        bool lastFlushWasBuffered = false;
        public void Flush(bool canBuffer = false)
        {
            if (quadsInBatch == 0) return;
            if (currentTexture == null) throw new InvalidOperationException("currentTexture is null");

            // When the previous flush was bufferable, draw state should stay the same.
            if (!lastFlushWasBuffered)
            {
                var combinedMatrix = transformMatrix * Camera.ProjectionView;
                GL.UniformMatrix4(combinedMatrixLocation, false, ref combinedMatrix);

                var samplerUnit = CustomTextureBind != null ? CustomTextureBind(currentTexture) : DrawState.BindTexture(currentTexture);
                if (currentSamplerUnit != samplerUnit)
                {
                    currentSamplerUnit = samplerUnit;
                    GL.Uniform1(textureUniformLocation, currentSamplerUnit);
                }

                FlushAction?.Invoke();
            }

            primitiveStreamer.Render(PrimitiveType.Quads, primitives, quadsInBatch, quadsInBatch * VertexPerQuad, canBuffer);

            currentLargestBatch += quadsInBatch;
            if (!canBuffer)
            {
                LargestBatch = Math.Max(LargestBatch, currentLargestBatch);
                currentLargestBatch = 0;
            }

            quadsInBatch = 0;
            FlushedBufferCount++;

            lastFlushWasBuffered = canBuffer;
        }
        public void Draw(ref QuadPrimitive quad, Texture2dRegion texture)
        {
            if (!rendering) throw new InvalidOperationException("Not rendering");
            if (texture == null) throw new ArgumentNullException(nameof(texture));

            if (currentTexture != texture.BindableTexture)
            {
                DrawState.FlushRenderer();
                currentTexture = texture.BindableTexture;
            }
            else if (quadsInBatch == maxQuadsPerBatch)
                DrawState.FlushRenderer(true);

            primitives[quadsInBatch] = quad;

            RenderedQuadCount++;
            quadsInBatch++;
        }
    }
}