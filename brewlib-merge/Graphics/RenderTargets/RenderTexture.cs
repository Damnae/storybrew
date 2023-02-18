using BrewLib.Graphics.Textures;
using OpenTK.Graphics.OpenGL;
using System;
using System.Diagnostics;

namespace BrewLib.Graphics.RenderTargets
{
    public class RenderTexture : IDisposable
    {
        public Texture2d Texture { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }

        public event EventHandler OnChanged;

        readonly RenderbufferStorage storage;
        public RenderbufferStorage Storage => storage;

        public RenderTexture(RenderbufferStorage storage) => this.storage = storage;
        public RenderTexture(int width, int height, RenderbufferStorage storage)
        {
            Width = width;
            Height = height;
            this.storage = storage;

            initialize(width, height);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        public void Dispose(bool disposing)
        {
            if (!disposing) return;

            clear();
        }
        public void Resize(int width, int height)
        {
            if (width == Width && height == Height) return;

            clear();
            initialize(width, height);

            OnChanged?.Invoke(this, EventArgs.Empty);
        }
        void initialize(int width, int height)
        {
            Width = width;
            Height = height;

            PixelInternalFormat pixelInternalFormat = (PixelInternalFormat)storage;
            PixelFormat pixelFormat;
            PixelType pixelType;

            switch (storage)
            {
                case RenderbufferStorage.DepthComponent:
                case RenderbufferStorage.DepthComponent16:
                case RenderbufferStorage.DepthComponent24:
                case RenderbufferStorage.DepthComponent32:
                case RenderbufferStorage.DepthComponent32f:
                    pixelFormat = PixelFormat.DepthComponent;
                    pixelType = PixelType.Float;
                    break;

                case RenderbufferStorage.DepthStencil:
                case RenderbufferStorage.Depth32fStencil8:
                case RenderbufferStorage.Depth24Stencil8:
                    pixelFormat = PixelFormat.DepthStencil;
                    pixelType = PixelType.UnsignedInt248;
                    break;

                case RenderbufferStorage.StencilIndex1:
                case RenderbufferStorage.StencilIndex4:
                case RenderbufferStorage.StencilIndex8:
                case RenderbufferStorage.StencilIndex16:
                    pixelFormat = PixelFormat.StencilIndex;
                    pixelType = PixelType.Float;
                    break;

                default:
                    pixelFormat = PixelFormat.Rgba;
                    pixelType = PixelType.UnsignedByte;
                    break;
            }

            var textureId = GL.GenTexture();
            Texture = new Texture2d(textureId, Width, Height, "rendertexture");

            DrawState.BindPrimaryTexture(textureId);
            GL.TexImage2D(TextureTarget.Texture2D, 0, pixelInternalFormat, Width, Height, 0, pixelFormat, pixelType, IntPtr.Zero);
            DrawState.CheckError("creating a render texture's texture");

            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);

            DrawState.UnbindTexture(textureId);

            Debug.Print(width + "x" + height + " " + storage + " render texture created");
        }
        void clear()
        {
            Texture?.Dispose();
            Texture = null;
        }
    }
}