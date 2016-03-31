using OpenTK.Graphics.OpenGL;
using System;

namespace StorybrewEditor.Graphics.Textures
{
    public class Texture3d : Texture, IDisposable
    {
        private readonly int textureId;
        public int TextureId
        {
            get
            {
                if (disposedValue)
                    throw new ObjectDisposedException(GetType().FullName);
                return textureId;
            }
        }
        public TexturingModes TexturingMode => TexturingModes.Texturing3d;

        public readonly int Width, Height, Depth;

        public Texture3d(int textureId, int width, int height, int depth)
        {
            this.textureId = textureId;

            Width = width;
            Height = height;
            Depth = depth;
        }

        public void Update<T>(T[] pixels, int width, int height, int depth,
            PixelInternalFormat pixelInternalFormat, PixelFormat pixelFormat, PixelType pixelType) where T : struct
        {
            try
            {
                DrawState.BindPrimaryTexture(textureId, TexturingModes.Texturing3d);
                GL.TexImage3D(TextureTarget.Texture3D, 0, pixelInternalFormat, width, height, depth, 0, pixelFormat, pixelType, pixels);

                DrawState.CheckError("updating texture");
            }
            finally
            {
                DrawState.UnbindTexture(textureId);
            }
        }

        public override string ToString()
        {
            return string.Format("Texture3d#{0} ({1}x{2}x{3})", textureId, Width, Height, Depth);
        }

        #region IDisposable Support

        private bool disposedValue = false;
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    DrawState.UnbindTexture(this);
                }
                GL.DeleteTexture(textureId);
                disposedValue = true;
            }
        }

        ~Texture3d()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion

        public static Texture3d Create(int width, int height, int depth,
            PixelInternalFormat pixelInternalFormat = PixelInternalFormat.Rgba,
            PixelFormat pixelFormat = PixelFormat.Bgra,
            PixelType pixelType = PixelType.UnsignedByte)
        {
            var textureId = GL.GenTexture();
            try
            {
                DrawState.BindPrimaryTexture(textureId, TexturingModes.Texturing3d);
                GL.TexImage3D(TextureTarget.Texture3D, 0, pixelInternalFormat, width, height, depth, 0, pixelFormat, pixelType, IntPtr.Zero);

                DrawState.CheckError("specifying texture");

                GL.TexParameter(TextureTarget.Texture3D, TextureParameterName.TextureMinFilter, (int)TextureMagFilter.Linear);
                GL.TexParameter(TextureTarget.Texture3D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
                GL.TexParameter(TextureTarget.Texture3D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat);
                GL.TexParameter(TextureTarget.Texture3D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat);

                DrawState.CheckError("setting texture parameters");
            }
            catch (Exception e)
            {
                GL.DeleteTexture(textureId);
                throw e;
            }
            finally
            {
                DrawState.UnbindTexture(textureId);
            }

            return new Texture3d(textureId, width, height, depth);
        }
    }
}
