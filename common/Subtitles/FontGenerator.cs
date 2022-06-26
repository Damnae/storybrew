using BrewLib.Util;
using OpenTK;
using OpenTK.Graphics;
using StorybrewCommon.Storyboarding;
using StorybrewCommon.Util;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.IO;
using System.Linq;
using Tiny;

namespace StorybrewCommon.Subtitles
{
    public class FontTexture
    {
        public string Path { get; }
        public bool IsEmpty => Path == null;
        public float OffsetX { get; }
        public float OffsetY { get; }
        public int BaseWidth { get; }
        public int BaseHeight { get; }
        public int Width { get; }
        public int Height { get; }

        public FontTexture(string path, float offsetX, float offsetY, int baseWidth, int baseHeight, int width, int height)
        {
            Path = path;
            OffsetX = offsetX;
            OffsetY = offsetY;
            BaseWidth = baseWidth;
            BaseHeight = baseHeight;
            Width = width;
            Height = height;
        }

        public Vector2 OffsetFor(OsbOrigin origin)
        {
            switch (origin)
            {
                default:
                case OsbOrigin.TopLeft: return new Vector2(OffsetX, OffsetY);
                case OsbOrigin.TopCentre: return new Vector2(OffsetX + Width * 0.5f, OffsetY);
                case OsbOrigin.TopRight: return new Vector2(OffsetX + Width, OffsetY);
                case OsbOrigin.CentreLeft: return new Vector2(OffsetX, OffsetY + Height * 0.5f);
                case OsbOrigin.Centre: return new Vector2(OffsetX + Width * 0.5f, OffsetY + Height * 0.5f);
                case OsbOrigin.CentreRight: return new Vector2(OffsetX + Width, OffsetY + Height * 0.5f);
                case OsbOrigin.BottomLeft: return new Vector2(OffsetX, OffsetY + Height);
                case OsbOrigin.BottomCentre: return new Vector2(OffsetX + Width * 0.5f, OffsetY + Height);
                case OsbOrigin.BottomRight: return new Vector2(OffsetX + Width, OffsetY + Height);
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
        public string Directory { get; }
        private readonly FontDescription description;
        private readonly FontEffect[] effects;
        private readonly string projectDirectory;
        private readonly string assetDirectory;

        private readonly Dictionary<string, FontTexture> textureCache = new Dictionary<string, FontTexture>();

        internal FontGenerator(string directory, FontDescription description, FontEffect[] effects, string projectDirectory, string assetDirectory)
        {
            Directory = directory;
            this.description = description;
            this.effects = effects;
            this.projectDirectory = projectDirectory;
            this.assetDirectory = assetDirectory;
        }

        public FontTexture GetTexture(string text)
        {
            if (!textureCache.TryGetValue(text, out FontTexture texture))
                textureCache.Add(text, texture = generateTexture(text));
            return texture;
        }

        private FontTexture generateTexture(string text)
        {
            var filename = text.Length == 1 ? $"{(int)text[0]:x4}.png" : $"_{textureCache.Count(l => l.Key.Length > 1):x3}.png";
            var bitmapPath = Path.Combine(assetDirectory, Directory, filename);

            System.IO.Directory.CreateDirectory(Path.GetDirectoryName(bitmapPath));

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
                                var r = new Random(textureCache.Count);
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
                                BrewLib.Util.Misc.WithRetries(() => trimmedBitmap.Save(bitmapPath, ImageFormat.Png));
                            }
                        }
                        else BrewLib.Util.Misc.WithRetries(() => bitmap.Save(bitmapPath, ImageFormat.Png));
                    }
                }
            }
            return new FontTexture(Path.Combine(Directory, filename), offsetX, offsetY, baseWidth, baseHeight, width, height);
        }

        internal void HandleCache(TinyToken cachedFontRoot)
        {
            if (!matches(cachedFontRoot))
                return;

            foreach (var cacheEntry in cachedFontRoot.Values<TinyObject>("Cache"))
            {
                var path = cacheEntry.Value<string>("Path");
                var hash = cacheEntry.Value<string>("Hash");

                var fullPath = Path.Combine(assetDirectory, path);
                if (!File.Exists(fullPath) || HashHelper.GetFileMd5(fullPath) != hash)
                    continue;

                var text = cacheEntry.Value<string>("Text");
                if (text.Contains('\ufffd'))
                {
                    Trace.WriteLine($"Ignoring invalid font texture \"{text}\" ({path})");
                    continue;
                }
                if (textureCache.ContainsKey(text))
                    throw new InvalidDataException($"The font texture for \"{text}\" ({path}) has been cached multiple times");

                textureCache.Add(text, new FontTexture(
                    path,
                    cacheEntry.Value<float>("OffsetX"),
                    cacheEntry.Value<float>("OffsetY"),
                    cacheEntry.Value<int>("BaseWidth"),
                    cacheEntry.Value<int>("BaseHeight"),
                    cacheEntry.Value<int>("Width"),
                    cacheEntry.Value<int>("Height")
                ));
            }
        }

        private bool matches(TinyToken cachedFontRoot)
        {
            if (cachedFontRoot.Value<string>("FontPath") == description.FontPath &&
                cachedFontRoot.Value<int>("FontSize") == description.FontSize &&
                MathUtil.FloatEquals(cachedFontRoot.Value<float>("ColorR"), description.Color.R, 0.00001f) &&
                MathUtil.FloatEquals(cachedFontRoot.Value<float>("ColorG"), description.Color.G, 0.00001f) &&
                MathUtil.FloatEquals(cachedFontRoot.Value<float>("ColorB"), description.Color.B, 0.00001f) &&
                MathUtil.FloatEquals(cachedFontRoot.Value<float>("ColorA"), description.Color.A, 0.00001f) &&
                MathUtil.FloatEquals(cachedFontRoot.Value<float>("PaddingX"), description.Padding.X, 0.00001f) &&
                MathUtil.FloatEquals(cachedFontRoot.Value<float>("PaddingY"), description.Padding.Y, 0.00001f) &&
                cachedFontRoot.Value<FontStyle>("FontStyle") == description.FontStyle &&
                cachedFontRoot.Value<bool>("TrimTransparency") == description.TrimTransparency &&
                cachedFontRoot.Value<bool>("EffectsOnly") == description.EffectsOnly &&
                cachedFontRoot.Value<bool>("Debug") == description.Debug)
            {
                var effectsRoot = cachedFontRoot.Value<TinyArray>("Effects");
                if (effectsRoot.Count != effects.Length)
                    return false;

                for (var i = 0; i < effects.Length; i++)
                    if (!effects[i].Matches(effectsRoot[i].Value<TinyToken>()))
                        return false;

                return true;
            }
            return false;
        }

        internal TinyObject ToTinyObject() => new TinyObject
        {
            { "FontPath", PathHelper.WithStandardSeparators(description.FontPath) },
            { "FontSize", description.FontSize },
            { "ColorR", description.Color.R },
            { "ColorG", description.Color.G },
            { "ColorB", description.Color.B },
            { "ColorA", description.Color.A },
            { "PaddingX", description.Padding.X },
            { "PaddingY", description.Padding.Y },
            { "FontStyle", description.FontStyle },
            { "TrimTransparency", description.TrimTransparency },
            { "EffectsOnly", description.EffectsOnly },
            { "Debug", description.Debug },
            { "Effects", effects.Select(e => e.ToTinyObject())},
            { "Cache", textureCache.Where(l => !l.Value.IsEmpty).Select(l => letterToTinyObject(l))},
        };

        private TinyObject letterToTinyObject(KeyValuePair<string, FontTexture> letterEntry) => new TinyObject
        {
            { "Text", letterEntry.Key },
            { "Path", PathHelper.WithStandardSeparators(letterEntry.Value.Path) },
            { "Hash", HashHelper.GetFileMd5(Path.Combine(assetDirectory, letterEntry.Value.Path)) },
            { "OffsetX", letterEntry.Value.OffsetX },
            { "OffsetY", letterEntry.Value.OffsetY },
            { "BaseWidth", letterEntry.Value.BaseWidth },
            { "BaseHeight", letterEntry.Value.BaseHeight },
            { "Width", letterEntry.Value.Width },
            { "Height", letterEntry.Value.Height },
        };
    }
}