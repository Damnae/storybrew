using OpenTK;
using OpenTK.Graphics;
using StorybrewCommon.Storyboarding;
using StorybrewCommon.Util;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.IO;

namespace StorybrewCommon.Subtitles
{
    public class FontTexture
    {
        private string path;
        public string Path => path;
        public bool IsEmpty => path == null;

        private float offsetX;
        public float OffsetX => offsetX;

        private float offsetY;
        public float OffsetY => offsetY;

        private int baseWidth;
        public int BaseWidth => baseWidth;

        private int baseHeight;
        public int BaseHeight => baseHeight;

        private int width;
        public int Width => width;

        private int height;
        public int Height => height;

        public FontTexture(string path, float offsetX, float offsetY, int baseWidth, int baseHeight, int width, int height)
        {
            this.path = path;
            this.offsetX = offsetX;
            this.offsetY = offsetY;
            this.baseWidth = baseWidth;
            this.baseHeight = baseHeight;
            this.width = width;
            this.height = height;
        }

        public Vector2 OffsetFor(OsbOrigin origin)
        {
            switch (origin)
            {
                default:
                case OsbOrigin.TopLeft: return new Vector2(offsetX, offsetY);
                case OsbOrigin.TopCentre: return new Vector2(offsetX + width * 0.5f, offsetY);
                case OsbOrigin.TopRight: return new Vector2(offsetX + width, offsetY);
                case OsbOrigin.CentreLeft: return new Vector2(offsetX, offsetY + height * 0.5f);
                case OsbOrigin.Centre: return new Vector2(offsetX + width * 0.5f, offsetY + height * 0.5f);
                case OsbOrigin.CentreRight: return new Vector2(offsetX + width, offsetY + height * 0.5f);
                case OsbOrigin.BottomLeft: return new Vector2(offsetX, offsetY + height);
                case OsbOrigin.BottomCentre: return new Vector2(offsetX + width * 0.5f, offsetY + height);
                case OsbOrigin.BottomRight: return new Vector2(offsetX + width, offsetY + height);
            }
        }
    }

    public class FontDescription
    {
        public string FontPath;
        public int FontSize = 76;
        public Color4 Color = new Color4(0, 0, 0, 100);
        public Vector2 Padding = Vector2.Zero;
        public FontStyle FontStyle = FontStyle.Regular;
        public bool TrimTransparency;
        public bool EffectsOnly;
        public bool Debug;
    }

    public class FontGenerator
    {
        private FontDescription description;
        private FontEffect[] effects;
        private string projectDirectory;
        private string mapsetDirectory;
        private string directory;

        private Dictionary<string, FontTexture> letters = new Dictionary<string, FontTexture>();

        public FontGenerator(string directory, FontDescription description, FontEffect[] effects, string projectDirectory, string mapsetDirectory)
        {
            this.description = description;
            this.effects = effects;
            this.projectDirectory = projectDirectory;
            this.mapsetDirectory = mapsetDirectory;
            this.directory = directory;
        }

        public FontTexture GetTexture(string text)
        {
            FontTexture texture;
            if (!letters.TryGetValue(text, out texture))
                letters.Add(text, texture = generateTexture(text));
            return texture;
        }

        private FontTexture generateTexture(string text)
        {
            var filename = text.Length == 1 ? $"{(int)text[0]:x4}.png" : $"_{letters.Count:x3}.png";
            var bitmapPath = Path.Combine(mapsetDirectory, directory, filename);

            Directory.CreateDirectory(Path.GetDirectoryName(bitmapPath));

            var fontPath = Path.Combine(projectDirectory, description.FontPath);
            if (!File.Exists(fontPath)) fontPath = description.FontPath;

            float offsetX = 0, offsetY = 0;
            int baseWidth, baseHeight, width, height;
            using (var graphics = Graphics.FromHwnd(IntPtr.Zero))
            using (var stringFormat = new StringFormat(StringFormat.GenericTypographic))
            using (var textBrush = new SolidBrush(Color.FromArgb(description.Color.ToArgb())))
            using (var fontCollection = new PrivateFontCollection())
            {
                graphics.TextRenderingHint = TextRenderingHint.AntiAlias;
                stringFormat.Alignment = StringAlignment.Center;
                stringFormat.FormatFlags = StringFormatFlags.FitBlackBox | StringFormatFlags.MeasureTrailingSpaces | StringFormatFlags.NoClip;

                FontFamily fontFamily = null;
                if (File.Exists(fontPath))
                {
                    fontCollection.AddFontFile(fontPath);
                    fontFamily = fontCollection.Families[0];
                }

                var dpiScale = 96f / graphics.DpiY;
                var fontStyle = description.FontStyle;
                using (var font = fontFamily != null ? new Font(fontFamily, description.FontSize * dpiScale, fontStyle) : new Font(fontPath, description.FontSize * dpiScale, fontStyle))
                {
                    var measuredSize = graphics.MeasureString(text, font, 0, stringFormat);
                    baseWidth = (int)Math.Ceiling(measuredSize.Width);
                    baseHeight = (int)Math.Ceiling(measuredSize.Height);

                    var effectsWidth = 0f;
                    var effectsHeight = 0f;
                    foreach (var effect in effects)
                    {
                        var effectSize = effect.Measure();
                        effectsWidth = Math.Max(effectsWidth, effectSize.X);
                        effectsHeight = Math.Max(effectsHeight, effectSize.Y);
                    }
                    width = (int)Math.Ceiling(baseWidth + effectsWidth + description.Padding.X * 2);
                    height = (int)Math.Ceiling(baseHeight + effectsHeight + description.Padding.Y * 2);

                    var paddingX = description.Padding.X + effectsWidth * 0.5f;
                    var paddingY = description.Padding.Y + effectsHeight * 0.5f;
                    var textX = paddingX + measuredSize.Width * 0.5f;
                    var textY = paddingY;

                    offsetX = -paddingX;
                    offsetY = -paddingY;

                    if (text.Length == 1 && char.IsWhiteSpace(text[0]) || width == 0 || height == 0)
                        return new FontTexture(null, offsetX, offsetY, baseWidth, baseHeight, width, height);

                    using (var bitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb))
                    {
                        using (var textGraphics = Graphics.FromImage(bitmap))
                        {
                            textGraphics.TextRenderingHint = graphics.TextRenderingHint;
                            textGraphics.SmoothingMode = SmoothingMode.HighQuality;
                            textGraphics.InterpolationMode = InterpolationMode.HighQualityBicubic;

                            if (description.Debug)
                            {
                                var r = new Random(letters.Count);
                                textGraphics.Clear(Color.FromArgb(r.Next(100, 255), r.Next(100, 255), r.Next(100, 255)));
                            }

                            foreach (var effect in effects)
                                if (!effect.Overlay)
                                    effect.Draw(bitmap, textGraphics, font, stringFormat, text, textX, textY);
                            if (!description.EffectsOnly)
                                textGraphics.DrawString(text, font, textBrush, textX, textY, stringFormat);
                            foreach (var effect in effects)
                                if (effect.Overlay)
                                    effect.Draw(bitmap, textGraphics, font, stringFormat, text, textX, textY);

                            if (description.Debug)
                                using (var pen = new Pen(Color.FromArgb(255, 0, 0)))
                                {
                                    textGraphics.DrawLine(pen, textX, textY, textX, textY + baseHeight);
                                    textGraphics.DrawLine(pen, textX - baseWidth * 0.5f, textY, textX + baseWidth * 0.5f, textY);
                                }
                        }

                        var bounds = description.TrimTransparency ? BitmapHelper.FindTransparencyBounds(bitmap) : null;
                        if (bounds != null && bounds != new Rectangle(0, 0, bitmap.Width, bitmap.Height))
                        {
                            var trimBounds = bounds.Value;
                            using (var trimmedBitmap = new Bitmap(trimBounds.Width, trimBounds.Height))
                            {
                                offsetX += trimBounds.Left;
                                offsetY += trimBounds.Top;
                                width = trimmedBitmap.Width;
                                height = trimmedBitmap.Height;
                                using (var trimGraphics = Graphics.FromImage(trimmedBitmap))
                                    trimGraphics.DrawImage(bitmap, 0, 0, trimBounds, GraphicsUnit.Pixel);
                                Misc.WithRetries(() => trimmedBitmap.Save(bitmapPath, ImageFormat.Png));
                            }
                        }
                        else Misc.WithRetries(() => bitmap.Save(bitmapPath, ImageFormat.Png));
                    }
                }
            }
            return new FontTexture(Path.Combine(directory, filename), offsetX, offsetY, baseWidth, baseHeight, width, height);
        }
    }
}