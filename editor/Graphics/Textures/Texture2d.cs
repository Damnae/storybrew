using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using StorybrewEditor.Util;
using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

namespace StorybrewEditor.Graphics.Textures
{
    public class Texture2d : Texture2dRegion, BindableTexture
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

        public Texture2d(int textureId, int width, int height, string description) : base(null, Box2.FromTLRB(0, 0, width, height), description)
        {
            this.textureId = textureId;
        }

        public void Update(Bitmap bitmap, int x, int y)
        {
            DrawState.BindPrimaryTexture(textureId, TexturingModes.Texturing2d);

            var bitmapData = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            GL.TexSubImage2D(TextureTarget.Texture2D, 0, x, y, bitmapData.Width, bitmapData.Height, OpenTK.Graphics.OpenGL.PixelFormat.Bgra, PixelType.UnsignedByte, bitmapData.Scan0);
            GL.Finish();
            bitmap.UnlockBits(bitmapData);

            DrawState.CheckError("updating texture");
        }

        public override string ToString()
            => $"Texture2d#{textureId} {Description} ({Width}x{Height})";

        #region IDisposable Support

        private bool disposedValue = false;
        protected override void Dispose(bool disposing)
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
            base.Dispose(disposing);
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

                var bitmapData = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

                GL.TexImage2D(TextureTarget.Texture2D, 0, sRgb ? PixelInternalFormat.SrgbAlpha : PixelInternalFormat.Rgba, bitmapData.Width, bitmapData.Height, 0, OpenTK.Graphics.OpenGL.PixelFormat.Bgra, PixelType.UnsignedByte, bitmapData.Scan0);
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
