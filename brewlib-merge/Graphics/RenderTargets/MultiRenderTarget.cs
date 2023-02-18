using OpenTK.Graphics.OpenGL;
using System;
using System.Diagnostics;
using System.Drawing;

namespace BrewLib.Graphics.RenderTargets
{
    public class MultiRenderTarget : IDisposable
    {
        RenderTexture[] renderTextures;
        RenderTexture depthStencilTexture;
        int frameBufferId = -1;
        bool started;

        int previousFrameBufferId;
        Rectangle previousViewport;

        bool initialized;
        int width, height;

        public MultiRenderTarget(RenderTexture[] renderTextures, RenderTexture depthStencilTexture)
        {
            if (renderTextures.Length > DrawState.MaxDrawBuffers) throw new ArgumentException("Can only draw to " + DrawState.MaxDrawBuffers + " buffers, requested " + renderTextures.Length);
            this.renderTextures = renderTextures;
            this.depthStencilTexture = depthStencilTexture;

            foreach (var renderTexture in renderTextures) renderTexture.OnChanged += renderTexture_OnChanged;
            if (depthStencilTexture != null) depthStencilTexture.OnChanged += renderTexture_OnChanged;
        }

        public void Begin(bool clear = true)
        {
            if (started) throw new InvalidOperationException("Already started");

            DrawState.FlushRenderer();
            if (!initialized) initialize();

            previousViewport = DrawState.Viewport;

            GL.GetInteger(GetPName.FramebufferBinding, out previousFrameBufferId);
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, frameBufferId);

            if (renderTextures.Length == 1) GL.DrawBuffer(DrawBufferMode.ColorAttachment0);
            else if (renderTextures.Length > 1)
            {
                var buffers = new DrawBuffersEnum[renderTextures.Length];
                for (int i = 0, size = renderTextures.Length; i < size; i++)
                {
                    Debug.Assert(DrawBuffersEnum.ColorAttachment0 + i <= DrawBuffersEnum.ColorAttachment15);
                    buffers[i] = DrawBuffersEnum.ColorAttachment0 + i;
                }
                GL.DrawBuffers(buffers.Length, buffers);
            }
            DrawState.CheckError("binding fbo");

            DrawState.Viewport = new Rectangle(0, 0, width, height);

            if (clear)
            {
                GL.ClearColor(0, 0, 0, 0);
                GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit | ClearBufferMask.StencilBufferBit);
            }

            started = true;
        }
        public void End()
        {
            if (!started) throw new InvalidOperationException("Not started");

            DrawState.FlushRenderer();

            GL.GetInteger(GetPName.FramebufferBinding, out int currentFrameBufferId);
            if (currentFrameBufferId != frameBufferId) throw new InvalidOperationException("Invalid current frame buffer");

            DrawState.Viewport = previousViewport;
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, previousFrameBufferId);

            started = false;
        }
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        public void Dispose(bool disposing)
        {
            if (!disposing) return;
            if (started) End();

            clear();

            foreach (var renderTexture in renderTextures) renderTexture.OnChanged -= renderTexture_OnChanged;
            renderTextures = null;

            if (depthStencilTexture != null)
            {
                depthStencilTexture.OnChanged -= renderTexture_OnChanged;
                depthStencilTexture = null;
            }
        }
        void initialize()
        {
            GL.GetInteger(GetPName.FramebufferBinding, out previousFrameBufferId);

            // Create the framebuffer
            frameBufferId = GL.GenFramebuffer();
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, frameBufferId);

            // Attach textures
            for (int i = 0, size = renderTextures.Length; i < size; i++)
            {
                var renderTexture = renderTextures[i];
                if (i == 0)
                {
                    width = renderTexture.Width;
                    height = renderTexture.Height;
                }
                else if (width != renderTexture.Width || height != renderTexture.Height)
                    throw new InvalidOperationException("Render textures must have the same size");

                var textureId = renderTexture.Texture.TextureId;
                Debug.Assert(FramebufferAttachment.ColorAttachment0 + i <= FramebufferAttachment.ColorAttachment15);
                GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0 + i, TextureTarget.Texture2D, textureId, 0);
            }

            if (depthStencilTexture != null)
            {
                if (width != depthStencilTexture.Width || height != depthStencilTexture.Height)
                    throw new InvalidOperationException("The depth / stencil texture must have the same size as the render textures");

                var textureId = depthStencilTexture.Texture.TextureId;
                switch (depthStencilTexture.Storage)
                {
                    case RenderbufferStorage.DepthComponent:
                    case RenderbufferStorage.DepthComponent16:
                    case RenderbufferStorage.DepthComponent24:
                    case RenderbufferStorage.DepthComponent32:
                    case RenderbufferStorage.DepthComponent32f:
                        GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment, TextureTarget.Texture2D, textureId, 0);
                        break;

                    case RenderbufferStorage.DepthStencil:
                    case RenderbufferStorage.Depth24Stencil8:
                    case RenderbufferStorage.Depth32fStencil8:
                        GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthStencilAttachment, TextureTarget.Texture2D, textureId, 0);
                        break;

                    case RenderbufferStorage.StencilIndex1:
                    case RenderbufferStorage.StencilIndex4:
                    case RenderbufferStorage.StencilIndex8:
                    case RenderbufferStorage.StencilIndex16:
                        GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.StencilAttachment, TextureTarget.Texture2D, textureId, 0);
                        break;

                    default:
                        throw new NotSupportedException(depthStencilTexture.Storage + " storage isn't supported for the depth / stencil texture.");
                }
            }

            // Check

            var status = GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer);
            if (status != FramebufferErrorCode.FramebufferComplete) throw new Exception("frame buffer couldn't be constructed: " + status);

            DrawState.CheckError("initializing multi render target");

            GL.BindFramebuffer(FramebufferTarget.Framebuffer, previousFrameBufferId);

            Debug.Print(width + "x" + height + " multi render target created");
            initialized = true;
        }
        void clear()
        {
            if (frameBufferId != -1)
            {
                GL.DeleteFramebuffer(frameBufferId);
                frameBufferId = -1;
            }
            initialized = false;
        }
        void renderTexture_OnChanged(object sender, EventArgs e) => clear();
    }
}