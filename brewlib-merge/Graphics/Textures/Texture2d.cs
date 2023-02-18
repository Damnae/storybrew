using BrewLib.Data;
using BrewLib.Util;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

namespace BrewLib.Graphics.Textures
{
    public class Texture2d : Texture2dRegion, BindableTexture
    {
        readonly int textureId;
        public int TextureId
        {
            get
            {
                if (disposedValue) throw new ObjectDisposedException(GetType().FullName);
                return textureId;
            }
        }
        public TexturingModes TexturingMode => TexturingModes.Texturing2d;

        public Texture2d(int textureId, int width, int height, string description) : base(null, Box2.FromTLRB(0, 0, width, height), description)
            => this.textureId = textureId;

        public override void Update(Bitmap bitmap, int x, int y, TextureOptions textureOptions)
        {
            if (bitmap.Width < 1 || bitmap.Height < 1)
                throw new InvalidOperationException($"Invalid bitmap size: {bitmap.Width}x{bitmap.Height}");

            if (x < 0 || y < 0 || x + bitmap.Width > Width || y + bitmap.Height > Height)
                throw new InvalidOperationException($"Invalid update bounds: {bitmap.Width}x{bitmap.Height} at {x},{y} overflows {Width}x{Height}");

            DrawState.BindPrimaryTexture(textureId, TexturingModes.Texturing2d);

            textureOptions = textureOptions ?? TextureOptions.Default;
            textureOptions.WithBitmap(bitmap, b =>
            {
                var bitmapData = b.LockBits(new Rectangle(0, 0, b.Width, b.Height), ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                GL.TexSubImage2D(TextureTarget.Texture2D, 0, x, y, bitmapData.Width, bitmapData.Height, OpenTK.Graphics.OpenGL.PixelFormat.Bgra, PixelType.UnsignedByte, bitmapData.Scan0);
                GL.Finish();
                b.UnlockBits(bitmapData);
            });

            DrawState.CheckError("updating texture");
        }

        public override string ToString() => $"Texture2d#{textureId} {Description} ({Width}x{Height})";

        #region IDisposable Support

        bool disposedValue = false;
        protected override void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing) DrawState.UnbindTexture(this);
                GL.DeleteTexture(textureId);
                disposedValue = true;
            }
            base.Dispose(disposing);
        }

        #endregion

        public static Bitmap LoadBitmap(string filename, ResourceContainer resourceContainer = null)
        {
            try
            {
                if (File.Exists(filename)) return (Bitmap)Image.FromFile(filename, false);
            }
            catch (OutOfMemoryException)
            {
                Trace.WriteLine($"Texture could not be loaded: {filename}");
                return null;
            }

            if (resourceContainer == null) return null;
            using (var stream = resourceContainer.GetStream(filename, ResourceSource.Embedded))
            {
                if (stream == null)
                {
                    Trace.WriteLine($"Texture not found: {filename}");
                    return null;
                }
                return (Bitmap)Image.FromStream(stream, false);
            }
        }

        public static TextureOptions LoadTextureOptions(string forBitmapFilename, ResourceContainer resourceContainer = null)
            => TextureOptions.Load(TextureOptions.GetOptionsFilename(forBitmapFilename), resourceContainer);

        public static Texture2d Load(string filename, ResourceContainer resourceContainer = null, TextureOptions textureOptions = null)
        {
            using (var bitmap = LoadBitmap(filename, resourceContainer)) return bitmap != null ?
                Load(bitmap, $"file:{filename}", textureOptions ?? LoadTextureOptions(filename, resourceContainer)) : null;
        }
        public static Texture2d Create(Color4 color, string description, int width = 1, int height = 1, TextureOptions textureOptions = null)
        {
            if (width < 1 || height < 1) throw new InvalidOperationException($"Invalid texture size: {width}x{height}");

            textureOptions = textureOptions ?? TextureOptions.Default;
            if (textureOptions.PreMultiply) color = color.Premultiply();

            var rgba = color.ToRgba();
            var data = new int[width * height];
            for (int i = 0; i < width * height; i++) data[i] = rgba;

            var textureId = GL.GenTexture();
            try
            {
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

            var textureWidth = Math.Min(DrawState.MaxTextureSize, bitmap.Width);
            var textureHeight = Math.Min(DrawState.MaxTextureSize, bitmap.Height);

            var textureId = GL.GenTexture();
            try
            {
                DrawState.BindTexture(textureId);

                textureOptions.WithBitmap(bitmap, b =>
                {
                    var bitmapData = b.LockBits(new Rectangle(0, 0, textureWidth, textureHeight), ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                    GL.TexImage2D(TextureTarget.Texture2D, 0, sRgb ? PixelInternalFormat.SrgbAlpha : PixelInternalFormat.Rgba, bitmapData.Width, bitmapData.Height, 0, OpenTK.Graphics.OpenGL.PixelFormat.Bgra, PixelType.UnsignedByte, bitmapData.Scan0);
                    if (textureOptions.GenerateMipmaps) GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);
                    GL.Finish();
                    b.UnlockBits(bitmapData);
                });
                DrawState.CheckError("specifying texture");

                textureOptions.ApplyParameters(TextureTarget.Texture2D);
            }
            catch (Exception)
            {
                GL.DeleteTexture(textureId);
                DrawState.UnbindTexture(textureId);
                throw;
            }
            return new Texture2d(textureId, textureWidth, textureHeight, description);
        }
    }
}