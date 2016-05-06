using StorybrewEditor.Util;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

namespace StorybrewEditor.Graphics.Textures
{
    public class Texture2d : Texture, IDisposable
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
        public TexturingModes TexturingMode => TexturingModes.Texturing2d;

        public readonly int Width, Height;
        public Vector2 Size => new Vector2(Width, Height);

        private string description;
        public string Description => description;

        public Texture2d(int textureId, int width, int height, string description)
        {
            this.textureId = textureId;
            this.description = description;

            Width = width;
            Height = height;
        }

        public override string ToString()
            => $"Texture2d#{textureId} {Description} ({Width}x{Height})";

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

        ~Texture2d()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion

        public static Texture2d Load(string filename, bool sRgb = false, bool allowResources = true)
        {
            if (File.Exists(filename))
                using (var stream = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.Read))
                    return Load(stream, filename, sRgb);

            if (!allowResources) return null;
            var resourceName = filename.Substring(0, filename.LastIndexOf(".")).Replace('-', '_');

            using (var bitmap = Resources.ResourceManager.GetObject(resourceName) as Bitmap)
                if (bitmap != null) return Load(bitmap, $"file:{filename}", sRgb);
                else
                {
                    Trace.WriteLine($"Texture not found: {filename} / {resourceName}");
                    return null;
                }
        }

        public static Texture2d Load(Stream stream, string filename, bool sRgb = false)
        {
            using (var bitmap = (Bitmap)Image.FromStream(stream, false, false))
                return Load(bitmap, $"file:{filename}", sRgb);
        }

        public static Texture2d Create(Color4 color, string description, int width = 1, int height = 1)
        {
            var textureId = GL.GenTexture();
            try
            {
                DrawState.BindTexture(textureId);
                var pixelInternalFormat = OpenTK.Graphics.OpenGL.PixelInternalFormat.Rgba;
                var pixelFormat = OpenTK.Graphics.OpenGL.PixelFormat.Rgba;
                var pixelType = OpenTK.Graphics.OpenGL.PixelType.UnsignedByte;

                var data = new int[width * height];
                for (int i = 0; i < width * height; i++)
                    data[i] = color.ToRgba();

                GL.TexImage2D(TextureTarget.Texture2D, 0, pixelInternalFormat, width, height, 0, pixelFormat, pixelType, data);

                DrawState.CheckError("specifying texture");

                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMagFilter.Linear);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);

                DrawState.CheckError("setting texture parameters");
            }
            catch (Exception)
            {
                GL.DeleteTexture(textureId);
                DrawState.UnbindTexture(textureId);
                throw;
            }

            return new Texture2d(textureId, width, height, description);
        }

        public static Texture2d Load(Bitmap bitmap, string description, bool sRgb = false)
        {
            if (bitmap == null) throw new ArgumentNullException(nameof(bitmap));

            sRgb &= DrawState.ColorCorrected;

            var textureId = GL.GenTexture();
            try
            {
                DrawState.BindTexture(textureId);
                OpenTK.Graphics.OpenGL.PixelInternalFormat pixelInternalFormat;
                OpenTK.Graphics.OpenGL.PixelFormat pixelFormat;
                OpenTK.Graphics.OpenGL.PixelType pixelType;

                switch (bitmap.PixelFormat)
                {
                    case System.Drawing.Imaging.PixelFormat.Format16bppArgb1555:
                    case System.Drawing.Imaging.PixelFormat.Format16bppRgb555:
                        pixelInternalFormat = OpenTK.Graphics.OpenGL.PixelInternalFormat.Rgb5A1;
                        pixelFormat = OpenTK.Graphics.OpenGL.PixelFormat.Bgr;
                        pixelType = OpenTK.Graphics.OpenGL.PixelType.UnsignedShort5551Ext;
                        break;
                    case System.Drawing.Imaging.PixelFormat.Format24bppRgb:
                        pixelInternalFormat = sRgb ? OpenTK.Graphics.OpenGL.PixelInternalFormat.Srgb8 : OpenTK.Graphics.OpenGL.PixelInternalFormat.Rgb8;
                        pixelFormat = OpenTK.Graphics.OpenGL.PixelFormat.Bgr;
                        pixelType = OpenTK.Graphics.OpenGL.PixelType.UnsignedByte;
                        break;
                    case System.Drawing.Imaging.PixelFormat.Format32bppRgb:
                    case System.Drawing.Imaging.PixelFormat.Canonical:
                    case System.Drawing.Imaging.PixelFormat.Format32bppArgb:
                        pixelInternalFormat = sRgb ? OpenTK.Graphics.OpenGL.PixelInternalFormat.SrgbAlpha : OpenTK.Graphics.OpenGL.PixelInternalFormat.Rgba;
                        pixelFormat = OpenTK.Graphics.OpenGL.PixelFormat.Bgra;
                        pixelType = OpenTK.Graphics.OpenGL.PixelType.UnsignedByte;
                        break;
                    default:
                        throw new Exception("Unsupported pixel format: " + bitmap.PixelFormat);
                }

                BitmapData bitmapData = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadOnly, bitmap.PixelFormat);
                GL.TexImage2D(TextureTarget.Texture2D, 0, pixelInternalFormat, bitmapData.Width, bitmapData.Height, 0, pixelFormat, pixelType, bitmapData.Scan0);
                GL.Finish();
                bitmap.UnlockBits(bitmapData);

                DrawState.CheckError("specifying texture");

                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMagFilter.Linear);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);

                DrawState.CheckError("setting texture parameters");
            }
            catch (Exception)
            {
                GL.DeleteTexture(textureId);
                DrawState.UnbindTexture(textureId);
                throw;
            }

            return new Texture2d(textureId, bitmap.Width, bitmap.Height, description);
        }
    }
}
