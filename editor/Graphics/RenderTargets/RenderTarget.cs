using StorybrewEditor.Graphics.Textures;
using OpenTK.Graphics.OpenGL;
using System;
using System.Diagnostics;
using System.Drawing;

namespace StorybrewEditor.Graphics.RenderTargets
{
    public class RenderTarget : IDisposable
    {
        public Texture2d Texture { get; private set; }
        public int Width { get; private set; }
        public int Height { get; private set; }

        private RenderbufferStorage? renderBufferType;

        private int textureId = -1;
        private int frameBufferId = -1;
        private int renderBufferId = -1;
        private bool started;

        private int previousFrameBufferId;
        private Rectangle previousViewport;

        public RenderTarget(RenderbufferStorage? renderBufferType = null)
        {
            this.renderBufferType = renderBufferType;
        }

        public RenderTarget(int width, int height, RenderbufferStorage? renderBufferType = null)
        {
            this.renderBufferType = renderBufferType;
            initialize(width, height);
        }

        public void Begin(bool clear = true)
        {
            if (started) throw new InvalidOperationException("Already started");
            if (textureId == -1) throw new InvalidOperationException("Not initialized");

            DrawState.FlushRenderer();

            previousViewport = DrawState.Viewport;

            GL.GetInteger(GetPName.FramebufferBinding, out previousFrameBufferId);
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, frameBufferId);
            GL.DrawBuffer(DrawBufferMode.ColorAttachment0);
            DrawState.CheckError("binding fbo");

            DrawState.Viewport = new Rectangle(0, 0, Width, Height);

            if (clear)
            {
                GL.ClearColor(0, 0, 0, 0);
                GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit | ClearBufferMask.StencilBufferBit);
            }

            started = true;
        }

        public void Resize(int width, int height)
        {
            if (started) throw new InvalidOperationException("Can't resize while started");
            if (width == Width && height == Height) return;

            clear();
            initialize(width, height);
        }

        public void End()
        {
            if (!started) throw new InvalidOperationException("Not started");

            DrawState.FlushRenderer();

            int currentFrameBufferId;
            GL.GetInteger(GetPName.FramebufferBinding, out currentFrameBufferId);
            if (currentFrameBufferId != frameBufferId)
                throw new InvalidOperationException("Invalid current frame buffer");

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
            if (!disposing)
                return;

            if (started)
                End();

            clear();
        }

        private void initialize(int width, int height)
        {
            Width = width;
            Height = height;

            textureId = GL.GenTexture();
            Texture = new Texture2d(textureId, width, height);

            DrawState.BindPrimaryTexture(textureId);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, width, height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, IntPtr.Zero);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);

            GL.GetInteger(GetPName.FramebufferBinding, out previousFrameBufferId);

            frameBufferId = GL.GenFramebuffer();
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, frameBufferId);
            GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2D, textureId, 0);
            DrawState.CheckError("creating fbo");

            if (renderBufferType != null)
            {
                renderBufferId = GL.GenRenderbuffer();
                GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, renderBufferId);
                GL.RenderbufferStorage(RenderbufferTarget.Renderbuffer, renderBufferType.Value, Width, Height);

                switch (renderBufferType.Value)
                {
                    case RenderbufferStorage.DepthComponent:
                    case RenderbufferStorage.DepthComponent16:
                    case RenderbufferStorage.DepthComponent24:
                    case RenderbufferStorage.DepthComponent32:
                    case RenderbufferStorage.DepthComponent32f:
                        GL.FramebufferRenderbuffer(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment, RenderbufferTarget.Renderbuffer, renderBufferId);
                        break;
                    case RenderbufferStorage.DepthStencil:
                    case RenderbufferStorage.Depth24Stencil8:
                    case RenderbufferStorage.Depth32fStencil8:
                        GL.FramebufferRenderbuffer(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthStencilAttachment, RenderbufferTarget.Renderbuffer, renderBufferId);
                        break;
                    case RenderbufferStorage.StencilIndex1:
                    case RenderbufferStorage.StencilIndex4:
                    case RenderbufferStorage.StencilIndex8:
                    case RenderbufferStorage.StencilIndex16:
                        GL.FramebufferRenderbuffer(FramebufferTarget.Framebuffer, FramebufferAttachment.StencilAttachment, RenderbufferTarget.Renderbuffer, renderBufferId);
                        break;
                    default:
                        throw new NotSupportedException("renderBufferType " + renderBufferType.Value + " isn't supported.");
                }
            }

            var status = GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer);
            if (status != FramebufferErrorCode.FramebufferComplete)
                throw new Exception("frame buffer couldn't be constructed: " + status);

            DrawState.UnbindTexture(textureId);
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, previousFrameBufferId);

            Debug.Print(width + "x" + height + " render target created");
        }

        private void clear()
        {
            if (frameBufferId != -1)
            {
                GL.DeleteFramebuffer(frameBufferId);
                frameBufferId = -1;
            }

            if (renderBufferId != -1)
            {
                GL.DeleteRenderbuffer(renderBufferId);
                renderBufferId = -1;
            }

            Texture?.Dispose();
            Texture = null;
        }
    }
}
