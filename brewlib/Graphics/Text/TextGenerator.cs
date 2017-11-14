using BrewLib.Graphics.Textures;
using BrewLib.Util;
using OpenTK;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.Resources;
using System.Runtime.InteropServices;

namespace BrewLib.Graphics.Text
{
    public class TextGenerator : IDisposable
    {
        private const bool debugFont = false;
        private static int debugSeed = 0;

        private SolidBrush textBrush = new SolidBrush(Color.FromArgb(255, 255, 255, 255));
        private SolidBrush shadowBrush = new SolidBrush(Color.FromArgb(220, 0, 0, 0));
        private Dictionary<string, Font> fonts = new Dictionary<string, Font>();
        private Dictionary<string, FontFamily> fontFamilies = new Dictionary<string, FontFamily>();
        private Dictionary<string, PrivateFontCollection> fontCollections = new Dictionary<string, PrivateFontCollection>();
        private LinkedList<string> recentlyUsedFonts = new LinkedList<string>();

        private ResourceManager resourceManager;

        public TextGenerator(ResourceManager resourceManager)
        {
            this.resourceManager = resourceManager;
        }

        public Bitmap CreateBitmap(string text, string fontName, float fontSize, Vector2 maxSize, Vector2 padding, BoxAlignment alignment, StringTrimming trimming, out Vector2 textureSize, bool measureOnly)
        {
            if (string.IsNullOrEmpty(text)) text = " ";

            StringAlignment horizontalAlignment;
            switch (alignment & BoxAlignment.Horizontal)
            {
                case BoxAlignment.Left: horizontalAlignment = StringAlignment.Near; break;
                case BoxAlignment.Right: horizontalAlignment = StringAlignment.Far; break;
                default: horizontalAlignment = StringAlignment.Center; break;
            }

            StringAlignment verticalAlignment;
            switch (alignment & BoxAlignment.Vertical)
            {
                case BoxAlignment.Top: verticalAlignment = StringAlignment.Near; break;
                case BoxAlignment.Bottom: verticalAlignment = StringAlignment.Far; break;
                default: verticalAlignment = StringAlignment.Center; break;
            }

            using (System.Drawing.Graphics graphics = System.Drawing.Graphics.FromHwnd(IntPtr.Zero))
            using (StringFormat stringFormat = new StringFormat(StringFormat.GenericTypographic))
            {
                graphics.TextRenderingHint = TextRenderingHint.AntiAlias;
                stringFormat.Alignment = horizontalAlignment;
                stringFormat.LineAlignment = verticalAlignment;
                stringFormat.Trimming = trimming;
                stringFormat.FormatFlags = StringFormatFlags.FitBlackBox | StringFormatFlags.MeasureTrailingSpaces | StringFormatFlags.NoClip; // | StringFormatFlags.LineLimit

                var dpiScale = 96f / graphics.DpiY;
                var font = getFont(fontName, fontSize * dpiScale, FontStyle.Regular);

                var measuredSize = graphics.MeasureString(text, font, new SizeF(maxSize.X, maxSize.Y), stringFormat);
                var width = (int)(measuredSize.Width + padding.X * 2 + 1);
                var height = (int)(measuredSize.Height + padding.Y * 2 + 1);

                var offsetX = padding.X;
                var offsetY = padding.Y;

                textureSize = new Vector2(width, height);
                if (measureOnly) return null;

                var bitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb);
                try
                {
                    using (System.Drawing.Graphics textGraphics = System.Drawing.Graphics.FromImage(bitmap))
                    {
                        textGraphics.TextRenderingHint = graphics.TextRenderingHint;
                        textGraphics.SmoothingMode = SmoothingMode.HighQuality;
                        textGraphics.InterpolationMode = InterpolationMode.HighQualityBicubic;

                        if (debugFont)
                        {
                            var r = new Random(debugSeed++);
                            textGraphics.Clear(Color.FromArgb(r.Next(100, 255), r.Next(100, 255), r.Next(100, 255)));
                        }

                        textGraphics.DrawString(text, font, shadowBrush, new RectangleF(offsetX + 1, offsetY + 1, width, height), stringFormat);
                        textGraphics.DrawString(text, font, textBrush, new RectangleF(offsetX, offsetY, width, height), stringFormat);
                    }
                }
                catch (Exception)
                {
                    bitmap.Dispose();
                    throw;
                }
                return bitmap;
            }
        }

        public Texture2d CreateTexture(string text, string fontName, float fontSize, Vector2 maxSize, Vector2 padding, BoxAlignment alignment, StringTrimming trimming, out Vector2 textureSize)
        {
            using (var bitmap = CreateBitmap(text, fontName, fontSize, maxSize, padding, alignment, trimming, out textureSize, false))
                return Texture2d.Load(bitmap, $"text:{text}@{fontName}:{fontSize}");
        }

        private Font getFont(string name, float emSize, FontStyle style)
        {
            var resourceName = name.Replace(' ', '_').Replace('-', '_');
            if (resourceName.Contains("."))
                resourceName = resourceName.Substring(0, name.LastIndexOf("."));

            var identifier = $"{resourceName}|{emSize}|{(int)style}";

            if (fonts.TryGetValue(identifier, out Font font))
            {
                recentlyUsedFonts.Remove(identifier);
                recentlyUsedFonts.AddFirst(identifier);
                return font;
            }
            else recentlyUsedFonts.AddFirst(identifier);

            if (recentlyUsedFonts.Count > 64)
                while (recentlyUsedFonts.Count > 32)
                {
                    var lastFontIdentifier = recentlyUsedFonts.Last.Value;
                    recentlyUsedFonts.RemoveLast();

                    fonts[lastFontIdentifier].Dispose();
                    fonts.Remove(lastFontIdentifier);
                }

            if (!fontFamilies.TryGetValue(resourceName, out FontFamily fontFamily))
            {
                var bytes = (byte[])resourceManager.GetObject(resourceName);
                if (bytes != null)
                {
                    GCHandle pinnedArray = GCHandle.Alloc(bytes, GCHandleType.Pinned);
                    try
                    {
                        if (!fontCollections.TryGetValue(resourceName, out PrivateFontCollection fontCollection))
                            fontCollections.Add(resourceName, fontCollection = new PrivateFontCollection());

                        IntPtr ptr = pinnedArray.AddrOfPinnedObject();
                        fontCollection.AddMemoryFont(ptr, bytes.Length);

                        if (fontCollection.Families.Length == 1)
                        {
                            fontFamily = fontCollection.Families[0];
                            Trace.WriteLine($"Loaded font {fontFamily.Name} for {name}/{resourceName}");
                        }
                        else Trace.WriteLine($"Failed to load font {name}/{resourceName}: Expected one family, got {fontCollection.Families.Length}");
                    }
                    catch (Exception e)
                    {
                        Trace.WriteLine($"Failed to load font {name}/{resourceName}: {e.Message}");
                    }
                    finally
                    {
                        pinnedArray.Free();
                    }
                }
                fontFamilies.Add(resourceName, fontFamily);
            }

            if (fontFamily != null)
                font = new Font(fontFamily, emSize, style);
            else
            {
                font = new Font(name, emSize, style);
                Trace.WriteLine($"Using font system font for {name}/{resourceName}");
            }

            fonts.Add(identifier, font);
            return font;
        }

        #region IDisposable Support

        private bool disposedValue = false;
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    textBrush.Dispose();
                    shadowBrush.Dispose();
                    foreach (var entry in fonts)
                        entry.Value.Dispose();
                    foreach (var fontCollection in fontCollections.Values)
                        fontCollection.Dispose();
                }
                textBrush = null;
                shadowBrush = null;
                fonts = null;
                fontCollections = null;
                fontFamilies = null;

                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }

        #endregion
    }
}
