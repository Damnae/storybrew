using BrewLib.Graphics.Textures;
using OpenTK.Graphics.OpenGL;
using System;
using System.Diagnostics;
using System.Drawing;

namespace BrewLib.Graphics.RenderTargets
{
    public class RenderTarget : IDisposable
    {
        private int textureId = -1;
        private int frameBufferId = -1;
        private int renderBufferId = -1;
        private bool started;
        private bool valid;

        private int previousFrameBufferId;
        private Rectangle previousViewport;

        private Texture2d texture;
        public Texture2d Texture
        {
            get
            {
                validate();
                return texture;
            }
        }

        private int width;
        public int Width
        {
            get { return width; }
            set
            {
                if (width == value) return;
                invalidate();
                width = value;
            }
        }
        private int height;
        public int Height
        {
            get { return height; }
            set
            {
                if (height == value) return;
                invalidate();
                height = value;
            }
        }

        private PixelInternalFormat internalFormat;
        public PixelInternalFormat InternalFormat
        {
            get { return internalFormat; }
            set
            {
                if (internalFormat == value) return;
                invalidate();
                internalFormat = value;
            }
        }
        private PixelFormat pixelFormat;
        public PixelFormat PixelFormat
        {
            get { return pixelFormat; }
            set
            {
                if (pixelFormat == value) return;
                invalidate();
                pixelFormat = value;
            }
        }
        private PixelType pixelType;
        public PixelType PixelType
        {
            get { return pixelType; }
            set
            {
                if (pixelType == value) return;
                invalidate();
                pixelType = value;
            }
        }

        private RenderbufferStorage? renderBufferType;
        public RenderbufferStorage? RenderBufferType
        {
            get { return renderBufferType; }
            set
            {
                if (renderBufferType == value) return;
                invalidate();
                renderBufferType = value;
            }
        }

        private TextureMinFilter textureMinFilter = TextureMinFilter.Linear;
        public TextureMinFilter TextureMinFilter
        {
            get { return textureMinFilter; }
            set
            {
                if (textureMinFilter == value) return;
                invalidate();
                textureMinFilter = value;
            }
        }

        private TextureMagFilter textureMagFilter = TextureMagFilter.Linear;
        public TextureMagFilter TextureMagFilter
        {
            get { return textureMagFilter; }
            set
            {
                if (textureMagFilter == value) return;
                invalidate();
                textureMagFilter = value;
            }
        }

        public RenderTarget(RenderbufferStorage? renderBufferType = null)
            : this(0, 0)
        {
        }

        public RenderTarget(int width, int height, RenderbufferStorage? renderBufferType = null)
            : this(width, height, DrawState.ColorCorrected ? PixelInternalFormat.SrgbAlpha : PixelInternalFormat.Rgba, PixelFormat.Rgba, PixelType.UnsignedByte, renderBufferType)
        {
        }

        public RenderTarget(PixelInternalFormat internalFormat, PixelFormat pixelFormat, PixelType pixelType, RenderbufferStorage? renderBufferType = null)
            : this(0, 0, internalFormat, pixelFormat, pixelType, renderBufferType)
        {
        }

        public RenderTarget(int width, int height, PixelInternalFormat internalFormat, PixelFormat pixelFormat, PixelType pixelType, RenderbufferStorage? renderBufferType = null)
        {
            this.width = width;
            this.height = height;
            this.internalFormat = internalFormat;
            this.pixelFormat = pixelFormat;
            this.pixelType = pixelType;
            this.renderBufferType = renderBufferType;
        }

        public void Begin(bool clear = true)
        {
            if (started) throw new InvalidOperationException("Already started");

            DrawState.FlushRenderer();

            validate();

            previousViewport = DrawState.Viewport;

            GL.GetInteger(GetPName.FramebufferBinding, out previousFrameBufferId);
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, frameBufferId);
            GL.DrawBuffer(DrawBufferMode.ColorAttachment0);
            DrawState.CheckError("binding fbo");

            DrawState.Viewport = new Rectangle(0, 0, width, height);

            if (clear)
            {
                // XXX need GL.DepthMask(true); to clear depth, but setting it here may cause issues with renderstate cache
                GL.ClearColor(0, 0, 0, 0);
                GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit | ClearBufferMask.StencilBufferBit);
            }

            started = true;
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

        private void invalidate()
        {
            if (started) throw new InvalidOperationException("Cannot change the rendertarget while started");
            if (!valid) return;

            valid = false;
            clear();
        }

        private void validate()
        {
            if (valid) return;

            valid = true;
            initialize();
        }

        private void initialize()
        {
            textureId = GL.GenTexture();
            texture = new Texture2d(textureId, width, height, "rendertarget");

            DrawState.BindPrimaryTexture(textureId);
            GL.TexImage2D(TextureTarget.Texture2D, 0, internalFormat, width, height, 0, pixelFormat, pixelType, IntPtr.Zero);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)textureMagFilter);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)textureMinFilter);

            GL.GetInteger(GetPName.FramebufferBinding, out previousFrameBufferId);

            frameBufferId = GL.GenFramebuffer();
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, frameBufferId);
            GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2D, textureId, 0);
            DrawState.CheckError("creating fbo");

            if (renderBufferType != null)
            {
                renderBufferId = GL.GenRenderbuffer();
                GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, renderBufferId);
                GL.RenderbufferStorage(RenderbufferTarget.Renderbuffer, renderBufferType.Value, width, height);

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

            texture?.Dispose();
            texture = null;
        }
    }
}
