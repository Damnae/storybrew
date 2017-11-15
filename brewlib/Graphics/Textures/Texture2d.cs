using BrewLib.Util;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Resources;

namespace BrewLib.Graphics.Textures
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

        public static Bitmap LoadBitmap(string filename, ResourceManager resourceManager = null)
        {
            if (File.Exists(filename))
                return (Bitmap)Image.FromFile(filename, false);

            if (resourceManager == null) return null;
            var resourceName = filename.Substring(0, filename.LastIndexOf(".")).Replace('-', '_');

            var bitmap = resourceManager.GetObject(resourceName) as Bitmap;
            if (bitmap == null)
            {
                Trace.WriteLine($"Texture not found: {filename} / {resourceName}");
                return null;
            }
            return bitmap;
        }

        public static TextureOptions LoadTextureOptions(string forBitmapFilename, ResourceManager resourceManager = null)
        {
            try
            {
                return TextureOptions.Load(TextureOptions.GetOptionsFilename(forBitmapFilename), resourceManager);
            }
            catch (FileNotFoundException)
            {
                Trace.WriteLine($"No texture options for {forBitmapFilename}");
            }
            return null;
        }

        public static Texture2d Load(string filename, ResourceManager resourceManager = null, TextureOptions textureOptions = null)
        {
            using (var bitmap = LoadBitmap(filename, resourceManager))
                return bitmap != null ? Load(bitmap, $"file:{filename}", textureOptions ?? LoadTextureOptions(filename, resourceManager)) : null;
        }

        public static Texture2d Create(Color4 color, string description, int width = 1, int height = 1, TextureOptions textureOptions = null)
        {
            textureOptions = textureOptions ?? TextureOptions.Default;

            var textureId = GL.GenTexture();
            try
            {
                var data = new int[width * height];
                for (int i = 0; i < width * height; i++)
                    data[i] = color.ToRgba();

                DrawState.BindTexture(textureId);
                GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, width, height, 0, OpenTK.Graphics.OpenGL.PixelFormat.Rgba, PixelType.UnsignedByte, data);
                if (textureOptions.GenerateMipmaps)
                {
                    GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);
                    GL.Finish();
                }
                DrawState.CheckError("specifying texture");

                textureOptions.ApplyParameters(TextureTarget.Texture2D);
            }
            catch (Exception)
            {
                GL.DeleteTexture(textureId);
                DrawState.UnbindTexture(textureId);
                throw;
            }

            return new Texture2d(textureId, width, height, description);
        }

        public static Texture2d Load(Bitmap bitmap, string description, TextureOptions textureOptions = null)
        {
            if (bitmap == null) throw new ArgumentNullException(nameof(bitmap));

            textureOptions = textureOptions ?? TextureOptions.Default;
            var sRgb = textureOptions.Srgb && DrawState.ColorCorrected;

            var textureId = GL.GenTexture();
            try
            {
                DrawState.BindTexture(textureId);

                var bitmapData = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

                GL.TexImage2D(TextureTarget.Texture2D, 0, sRgb ? PixelInternalFormat.SrgbAlpha : PixelInternalFormat.Rgba, bitmapData.Width, bitmapData.Height, 0, OpenTK.Graphics.OpenGL.PixelFormat.Bgra, PixelType.UnsignedByte, bitmapData.Scan0);
                if (textureOptions.GenerateMipmaps)
                    GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);
                GL.Finish();
                bitmap.UnlockBits(bitmapData);
                DrawState.CheckError("specifying texture");

                textureOptions.ApplyParameters(TextureTarget.Texture2D);
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
