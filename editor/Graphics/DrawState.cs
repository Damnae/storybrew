using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using StorybrewEditor.Graphics.Cameras;
using StorybrewEditor.Graphics.Renderers;
using StorybrewEditor.Graphics.Text;
using StorybrewEditor.Graphics.Textures;
using StorybrewEditor.Util;
using System;
using System.Diagnostics;
using System.Drawing;
using System.Linq;

namespace StorybrewEditor.Graphics
{
    public static class DrawState
    {
        public const bool AllowShaders = true;
        public const bool UseSrgb = false;

        private static Version openGlVersion;
        private static Version glslVersion;
        private static string[] supportedExtensions;

        private static int maxDrawBuffers;
        public static int MaxDrawBuffers => maxDrawBuffers;

        private static bool colorCorrected;
        public static bool ColorCorrected => colorCorrected;

        public static int TextureBinds { get; private set; }

        public static void Initialize(int width, int height)
        {
            GL.Hint(HintTarget.PerspectiveCorrectionHint, HintMode.Nicest);
            GL.Disable(EnableCap.DepthTest);
            GL.Disable(EnableCap.Lighting);
            GL.Enable(EnableCap.Blend);

            if (UseSrgb && HasCapabilities(3, 0, "GL_ARB_framebuffer_object"))
            {
                int defaultFramebufferColorEncoding;
                GL.GetFramebufferAttachmentParameter(FramebufferTarget.Framebuffer, FramebufferAttachment.BackLeft, FramebufferParameterName.FramebufferAttachmentColorEncoding, out defaultFramebufferColorEncoding);
                if (defaultFramebufferColorEncoding == (int)0x8C40)
                {
                    GL.Enable(EnableCap.FramebufferSrgb);
                    colorCorrected = true;
                }
                else Trace.WriteLine("Warning: The default framebuffer isn't sRgb");
            }

            // glActiveTexture requires opengl 1.3
            maxFpTextureUnits = HasCapabilities(1, 3) ? GL.GetInteger(GetPName.MaxTextureUnits) : 1;
            maxTextureImageUnits = GL.GetInteger(GetPName.MaxTextureImageUnits);
            maxVertexTextureImageUnits = GL.GetInteger(GetPName.MaxVertexTextureImageUnits);
            maxGeometryTextureImageUnits = GL.GetInteger(GetPName.MaxGeometryTextureImageUnits);
            maxCombinedTextureImageUnits = GL.GetInteger(GetPName.MaxCombinedTextureImageUnits);
            maxTextureCoords = GL.GetInteger(GetPName.MaxTextureCoords);

            // glDrawBuffers requires opengl 2.0
            maxDrawBuffers = HasCapabilities(2, 0) ? GL.GetInteger(GetPName.MaxDrawBuffers) : 1;

            Trace.WriteLine($"texture units available: fp:{maxFpTextureUnits} ps:{maxTextureImageUnits} vs:{maxVertexTextureImageUnits} gs:{maxGeometryTextureImageUnits} combined:{maxCombinedTextureImageUnits} coords:{maxTextureCoords}");

            samplerTextureIds = new int[maxTextureImageUnits];
            samplerTexturingModes = new TexturingModes[maxTextureImageUnits];

            whitePixel = Texture2d.Create(Color4.White, "whitepixel");
            normalPixel = Texture2d.Create(new Color4(0.5f, 0.5f, 1, 1), "normalpixel");
            fontManager = new FontManager();

            Viewport = new Rectangle(0, 0, width, height);
            ProjViewMatrix = CameraOrtho.Default.ProjectionView;
            SetBlending(BlendingMode.Default);
        }

        public static void Cleanup()
        {
            normalPixel.Dispose();
            normalPixel = null;

            whitePixel.Dispose();
            whitePixel = null;

            fontManager.Dispose();
            fontManager = null;
        }

        public static void CompleteFrame()
        {
            Renderer = null;
        }

        private static Renderer renderer;
        public static Renderer Renderer
        {
            get { return renderer; }
            set
            {
                if (renderer == value)
                    return;

                flushingRenderer = true;
                renderer?.EndRendering();

                renderer = value;

                renderer?.BeginRendering();
                flushingRenderer = false;
            }
        }

        private static bool flushingRenderer;
        public static void FlushRenderer()
        {
            if (renderer == null || flushingRenderer)
                return;

            flushingRenderer = true;
            renderer.Flush();
            flushingRenderer = false;
        }

        #region Texture states

        private static Texture2d whitePixel;
        public static Texture2d WhitePixel => whitePixel;

        private static Texture2d normalPixel;
        public static Texture2d NormalPixel => normalPixel;

        private static int activeTextureUnit = 0;
        public static int ActiveTextureUnit
        {
            get { return activeTextureUnit; }
            set
            {
                if (activeTextureUnit == value) return;

                GL.ActiveTexture(TextureUnit.Texture0 + value);
                activeTextureUnit = value;
            }
        }

        private static int lastRecycledTextureUnit = -1;
        private static int[] samplerTextureIds;
        private static TexturingModes[] samplerTexturingModes;

        private static int maxFpTextureUnits;
        private static int maxTextureImageUnits;
        private static int maxVertexTextureImageUnits;
        private static int maxGeometryTextureImageUnits;
        private static int maxCombinedTextureImageUnits;
        private static int maxTextureCoords;

        public static void SetTexturingMode(int samplerIndex, TexturingModes mode)
        {
            var previousMode = samplerTexturingModes[samplerIndex];
            if (previousMode == mode) return;

            if (samplerTextureIds[samplerIndex] != 0)
                UnbindTexture(samplerTextureIds[samplerIndex]);

            // Only matters for the fixed pipeline
            if (samplerIndex < maxFpTextureUnits)
            {
                ActiveTextureUnit = samplerIndex;
                if (previousMode != TexturingModes.None)
                    GL.Disable((EnableCap)ToTextureTarget(previousMode));

                if (mode != TexturingModes.None)
                    GL.Enable((EnableCap)ToTextureTarget(mode));
            }

            samplerTexturingModes[samplerIndex] = mode;
        }

        /// <summary>
        /// Bind the texture in the first texture unit by its textureId and activate it
        /// </summary>
        public static void BindPrimaryTexture(int textureId, TexturingModes mode = TexturingModes.Texturing2d)
        {
            BindTexture(textureId, 0, mode);
        }

        /// <summary>
        /// Bind the texture to a specific texture unit using its textureId and activate it
        /// </summary>
        public static void BindTexture(int textureId, int samplerIndex = 0, TexturingModes mode = TexturingModes.Texturing2d)
        {
            if (textureId == 0) throw new ArgumentException("Use UnbindTexture instead");

            SetTexturingMode(samplerIndex, mode);
            ActiveTextureUnit = samplerIndex;

            if (samplerTextureIds[samplerIndex] != textureId)
            {
                GL.BindTexture(ToTextureTarget(mode), textureId);
                samplerTextureIds[samplerIndex] = textureId;

                //Debug.Print("Bound texture " + textureId + " (" + mode + ") to unit " + samplerIndex);
                TextureBinds++;
            }
        }

        /// <summary>
        /// Bind the texture in any texture unit, reusing previously bound textures when possible
        /// </summary>
        /// <param name="texture">the texture to bind</param>
        /// <param name="activate">whether to glActiveTexture the texture unit</param>
        /// <returns>the texture unit the texture is bound to</returns>
        public static int BindTexture(Texture texture, bool activate = false)
        {
            var samplerUnit = BindTextures(texture)[0];
            if (activate) ActiveTextureUnit = samplerUnit;
            return samplerUnit;
        }

        public static void UnbindTexture(Texture texture)
        {
            UnbindTexture(texture.TextureId);
        }

        public static void UnbindTexture(int textureId)
        {
            for (int samplerIndex = 0, samplerCount = samplerTextureIds.Length; samplerIndex < samplerCount; samplerIndex++)
            {
                if (samplerTextureIds[samplerIndex] == textureId)
                {
                    ActiveTextureUnit = samplerIndex;
                    GL.BindTexture(ToTextureTarget(samplerTexturingModes[samplerIndex]), 0);
                    samplerTextureIds[samplerIndex] = 0;

                    //Debug.Print("Unbound texture " + textureId + " from unit " + samplerIndex);
                }
            }
        }

        /// <summary>
        /// Bind the textures in any texture unit, reusing previously bound textures when possible.
        /// </summary>
        public static int[] BindTextures(params Texture[] textures)
        {
            int[] samplerIndexes = new int[textures.Length];
            int samplerCount = samplerTextureIds.Length;

            // Find already bound textures
            for (int textureIndex = 0, textureCount = textures.Length; textureIndex < textureCount; textureIndex++)
            {
                int textureId = textures[textureIndex].TextureId;

                samplerIndexes[textureIndex] = -1;
                for (int samplerIndex = 0; samplerIndex < samplerCount; samplerIndex++)
                {
                    if (samplerTextureIds[samplerIndex] == textureId)
                    {
                        samplerIndexes[textureIndex] = samplerIndex;
                        break;
                    }
                }
            }

            // Bind remaining textures
            for (int textureIndex = 0, textureCount = textures.Length; textureIndex < textureCount; textureIndex++)
            {
                if (samplerIndexes[textureIndex] != -1)
                    continue;

                var texture = textures[textureIndex];
                int textureId = texture.TextureId;

                var first = true;
                var samplerStartIndex = (lastRecycledTextureUnit + 1) % samplerCount;
                for (int samplerIndex = samplerStartIndex;
                        first || samplerIndex != samplerStartIndex;
                        samplerIndex = (samplerIndex + 1) % samplerCount)
                {
                    first = false;

                    bool isFreeSamplerUnit = true;
                    foreach (var usedIndex in samplerIndexes)
                    {
                        if (usedIndex == samplerIndex)
                        {
                            isFreeSamplerUnit = false;
                            break;
                        }
                    }

                    if (isFreeSamplerUnit)
                    {
                        BindTexture(textureId, samplerIndex, texture.TexturingMode);
                        samplerIndexes[textureIndex] = samplerIndex;
                        lastRecycledTextureUnit = samplerIndex;
                        break;
                    }
                }
            }
            return samplerIndexes;
        }

        #endregion

        #region Other states

        private static Rectangle viewport;
        public static Rectangle Viewport
        {
            get { return viewport; }
            set
            {
                if (viewport == value)
                    return;

                viewport = value;

                GL.Viewport(viewport);
                ViewportChanged?.Invoke();
            }
        }
        public delegate void ViewportChangedEventHandler();
        public static event ViewportChangedEventHandler ViewportChanged;

        private static Rectangle? clipRegion;
        public static Rectangle? ClipRegion
        {
            get { return clipRegion; }
            private set
            {
                if (clipRegion == value)
                    return;

                FlushRenderer();

                clipRegion = value;
                if (clipRegion.HasValue)
                {
                    var actualClipRegion = Rectangle.Intersect(clipRegion.Value, viewport);

                    GL.Enable(EnableCap.ScissorTest);
                    GL.Scissor(actualClipRegion.X, actualClipRegion.Y, actualClipRegion.Width, actualClipRegion.Height);
                }
                else GL.Disable(EnableCap.ScissorTest);
            }
        }

        public static IDisposable Clip(Rectangle? newRegion)
        {
            var previousClipRegion = clipRegion;
            ClipRegion = clipRegion.HasValue && newRegion.HasValue ? Rectangle.Intersect(clipRegion.Value, newRegion.Value) : newRegion;
            return new ActionDisposable(() => ClipRegion = previousClipRegion);
        }

        public static IDisposable Clip(Box2 bounds, Camera camera)
        {
            var screenBounds = camera.ToScreen(bounds);
            var clipRectangle = new Rectangle(
                (int)Math.Round(screenBounds.Left),
                viewport.Height - (int)Math.Round(screenBounds.Top + screenBounds.Height),
                (int)Math.Round(screenBounds.Width),
                (int)Math.Round(screenBounds.Height));
            return Clip(clipRectangle);
        }

        private static Matrix4 projViewMatrix;
        public static Matrix4 ProjViewMatrix
        {
            get { return projViewMatrix; }
            set
            {
                if (projViewMatrix == value)
                    return;

                FlushRenderer();

                projViewMatrix = value;

                GL.MatrixMode(MatrixMode.Projection);
                GL.LoadMatrix(ref projViewMatrix);
                CheckError("setting up projection / view");
            }
        }

        private static int programId;
        public static int ProgramId
        {
            get { return programId; }
            set
            {
                if (programId == value)
                    return;

                programId = value;
                GL.UseProgram(programId);
            }
        }

        private static BlendingFactorSrc srcBlendingColor;
        private static BlendingFactorDest destBlendingColor;
        private static BlendingFactorSrc srcBlendingAlpha;
        private static BlendingFactorDest destBlendingAlpha;

        public static void SetBlending(BlendingFactorSrc src, BlendingFactorDest dest)
        {
            if (srcBlendingColor == src && destBlendingColor == dest
                && srcBlendingAlpha == src && destBlendingAlpha == dest)
                return;

            FlushRenderer();
            GL.BlendFunc(src, dest);

            srcBlendingColor = src;
            destBlendingColor = dest;
            srcBlendingAlpha = src;
            destBlendingAlpha = dest;
        }

        public static void SetBlending(BlendingFactorSrc src, BlendingFactorDest dest, BlendingFactorSrc alphaSrc, BlendingFactorDest alphaDest)
        {
            if (srcBlendingColor == src && destBlendingColor == dest
                && srcBlendingAlpha == alphaSrc && destBlendingAlpha == alphaDest)
                return;

            FlushRenderer();
            GL.BlendFuncSeparate(src, dest, alphaSrc, alphaDest);

            srcBlendingColor = src;
            destBlendingColor = dest;
            srcBlendingAlpha = alphaSrc;
            destBlendingAlpha = alphaDest;
        }

        public static void SetBlending(BlendingMode blendingMode)
        {
            switch (blendingMode)
            {
                case BlendingMode.Default:
                    SetBlending(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha); break;
                case BlendingMode.Color:
                    SetBlending(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha, BlendingFactorSrc.Zero, BlendingFactorDest.One); break;
                case BlendingMode.Additive:
                    SetBlending(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.One); break;
                case BlendingMode.Premultiply:
                    SetBlending(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha, BlendingFactorSrc.One, BlendingFactorDest.OneMinusSrcAlpha); break;
                case BlendingMode.Premultiplied:
                    SetBlending(BlendingFactorSrc.One, BlendingFactorDest.OneMinusSrcAlpha); break;
            }
        }

        private static BlendEquationMode blendColorEquation = BlendEquationMode.FuncAdd;
        private static BlendEquationMode blendAlphaEquation = BlendEquationMode.FuncAdd;

        public static void SetBlendingEquation(BlendEquationMode mode)
        {
            if (blendColorEquation == mode && blendColorEquation == mode) return;

            FlushRenderer();
            GL.BlendEquation(mode);

            blendColorEquation = blendAlphaEquation = mode;
        }

        public static void SetBlendingEquation(BlendEquationMode colorMode, BlendEquationMode alphaMode)
        {
            if (blendColorEquation == colorMode && blendColorEquation == alphaMode) return;

            FlushRenderer();
            GL.BlendEquationSeparate(colorMode, alphaMode);

            blendColorEquation = colorMode;
            blendAlphaEquation = alphaMode;
        }

        #endregion

        #region Utilities

        private static FontManager fontManager;
        public static FontManager FontManager => fontManager;

        internal static bool HasCapabilities(int major, int minor, params string[] extensions)
        {
            if (openGlVersion == null)
            {
                var openGlVersionString = GL.GetString(StringName.Version);
                openGlVersion = new Version(openGlVersionString.Split(' ')[0]);
                Trace.WriteLine("gl version: " + openGlVersionString);
                CheckError("retrieving openGL version");
            }
            return openGlVersion >= new Version(major, minor) || HasExtensions(extensions);
        }

        internal static bool HasExtensions(params string[] extensions)
        {
            if (supportedExtensions == null)
            {
                var extensionsString = GL.GetString(StringName.Extensions);
                supportedExtensions = extensionsString.Split(' ');
                Trace.WriteLine("extensions: " + extensionsString);
                CheckError("retrieving available openGL extensions");
            }
            foreach (var extension in extensions)
            {
                if (!supportedExtensions.Contains(extension))
                    return false;
            }
            return true;
        }

        internal static bool HasShaderCapabilities(int major, int minor)
        {
            if (glslVersion == null)
            {
                var glslVersionString = GL.GetString(StringName.ShadingLanguageVersion);
                glslVersion = string.IsNullOrEmpty(glslVersionString) ? new Version() : new Version(glslVersionString.Split(' ')[0]);
                Trace.WriteLine("glsl version: " + glslVersionString);
                CheckError("retrieving GLSL version");
            }
            return glslVersion >= new Version(major, minor);
        }

        public static TextureTarget ToTextureTarget(TexturingModes mode)
        {
            switch (mode)
            {
                case TexturingModes.Texturing2d: return TextureTarget.Texture2D;
                case TexturingModes.Texturing3d: return TextureTarget.Texture3D;
                default: throw new InvalidOperationException("Not texture target matches the texturing mode " + mode);
            }
        }

        public static void CheckError(string context = null, bool alwaysThrow = false)
        {
            var error = GL.GetError();
            if (alwaysThrow || error != ErrorCode.NoError)
                throw new Exception(
                    (context != null ? "openGL error while " + context : "openGL error") +
                    (error != ErrorCode.NoError ? ": " + error.ToString() : string.Empty));
        }

        #endregion
    }

    public enum BlendingMode
    {
        Default,
        Color,
        Additive,
        Premultiply,
        Premultiplied,
    }

    public enum TexturingModes
    {
        None,
        Texturing2d,
        Texturing3d
    }
}
