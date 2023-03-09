using BrewLib.Util;
using OpenTK;
using OpenTK.Graphics;
using StorybrewCommon.Storyboarding;
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
    ///<summary> Stores information about a font image. </summary>
    public class FontTexture
    {
        ///<summary> The path to the font texture. </summary>
        public string Path { get; }

        ///<returns> True if the path does not exist, else returns false. </returns>
        public bool IsEmpty => Path == null;

        ///<summary> The texture offset in X-units. </summary>
        public float OffsetX { get; }

        ///<summary> The texture offset in Y-units. </summary>
        public float OffsetY { get; }

        ///<summary> The base width of the texture in pixels. </summary>
        public int BaseWidth { get; }

        ///<summary> The base height of the texture in pixels. </summary>
        public int BaseHeight { get; }

        ///<summary> The actual width of the texture in pixels. </summary>
        public int Width { get; }

        ///<summary> The actual width of the texture in pixels. </summary>
        public int Height { get; }

        ///<summary> Creates a new <see cref="FontTexture"/> storing information of the font. </summary>
        ///<param name="path"> The path to the font texture. </param>
        ///<param name="offsetX"> The texture offset in X-units. </param>
        ///<param name="offsetY"> The texture offset in Y-units. </param>
        ///<param name="baseWidth"> The base width of the texture in pixels. </param>
        ///<param name="baseHeight"> The base height of the texture in pixels. </param>
        ///<param name="width"> The actual width of the texture in pixels. </param>
        ///<param name="height"> The actual width of the texture in pixels. </param>
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

        ///<summary> Gets the font offset for the given <see cref="OsbOrigin"/>. </summary>
        public Vector2 OffsetFor(OsbOrigin origin)
        {
            switch (origin)
            {
                default:
                case OsbOrigin.TopLeft: return new Vector2(OffsetX, OffsetY);
                case OsbOrigin.TopCentre: return new Vector2(OffsetX + Width * .5f, OffsetY);
                case OsbOrigin.TopRight: return new Vector2(OffsetX + Width, OffsetY);
                case OsbOrigin.CentreLeft: return new Vector2(OffsetX, OffsetY + Height * .5f);
                case OsbOrigin.Centre: return new Vector2(OffsetX + Width * .5f, OffsetY + Height * .5f);
                case OsbOrigin.CentreRight: return new Vector2(OffsetX + Width, OffsetY + Height * .5f);
                case OsbOrigin.BottomLeft: return new Vector2(OffsetX, OffsetY + Height);
                case OsbOrigin.BottomCentre: return new Vector2(OffsetX + Width * .5f, OffsetY + Height);
                case OsbOrigin.BottomRight: return new Vector2(OffsetX + Width, OffsetY + Height);
            }
        }
    }

    /// <summary> Stores information about a font's looks. </summary>
    public class FontDescription
    {
        ///<summary> The path to the font texture. </summary>
        public string FontPath;

        ///<summary> The relative size of the font texture. </summary>
        public int FontSize = 76;

        ///<summary> The coloring tint of the font texture. </summary>
        public Color4 Color = new Color4(0, 0, 0, 100);

        ///<summary> How much extra space is allocated around the text when generating it. </summary>
        public Vector2 Padding = Vector2.Zero;

        /// <summary> The format/style of the font texture (for example: bold, italics, etc). </summary>
        public FontStyle FontStyle = FontStyle.Regular;

        ///<summary> <see cref="bool"/> toggle to trim extra transparent space around the texture. Should always be true. </summary>
        public bool TrimTransparency;

        ///<summary> <see cref="bool"/> toggle to only draw the glow, outline and shadow. </summary>
        public bool EffectsOnly;

        ///<summary> <see cref="bool"/> toggle to draw a randomly colored background behind the textures. </summary>
        public bool Debug;
    }

    ///<summary> Creates font textures. </summary>
    public class FontGenerator
    {
        /// <summary> The directory to  </summary>
        public string Directory { get; }
        readonly FontDescription description;
        readonly FontEffect[] effects;
        readonly string projectDirectory, assetDirectory;
        readonly bool optimize;

        readonly Dictionary<string, FontTexture> textureCache = new Dictionary<string, FontTexture>();

        internal FontGenerator(string directory, FontDescription description, FontEffect[] effects, string projectDirectory, string assetDirectory, bool optimize)
        {
            Directory = directory;
            this.description = description;
            this.effects = effects;
            this.projectDirectory = projectDirectory;
            this.assetDirectory = assetDirectory;
            this.optimize = optimize;
        }

        ///<summary> Gets the texture path of a texture sprite. </summary>
        public FontTexture GetTexture(string text)
        {
            if (!textureCache.TryGetValue(text, out FontTexture texture)) textureCache.Add(text, texture = generateTexture(text));
            return texture;
        }
        FontTexture generateTexture(string text)
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

                var dpiScale = 96 / graphics.DpiY;
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

                    var paddingX = description.Padding.X + effectsWidth / 2;
                    var paddingY = description.Padding.Y + effectsHeight / 2;
                    var textX = paddingX + measuredSize.Width / 2;
                    var textY = paddingY;

                    offsetX = -paddingX;
                    offsetY = -paddingY;

                    if (text.Length == 1 && char.IsWhiteSpace(text[0]) || width == 0 || height == 0) return new FontTexture(null, offsetX, offsetY, baseWidth, baseHeight, width, height);

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

                            foreach (var effect in effects) if (!effect.Overlay) effect.Draw(bitmap, textGraphics, font, stringFormat, text, textX, textY);
                            if (!description.EffectsOnly) textGraphics.DrawString(text, font, textBrush, textX, textY, stringFormat);
                            foreach (var effect in effects) if (effect.Overlay) effect.Draw(bitmap, textGraphics, font, stringFormat, text, textX, textY);

                            if (description.Debug) using (var pen = new Pen(Color.FromArgb(255, 0, 0)))
                            {
                                textGraphics.DrawLine(pen, textX, textY, textX, textY + baseHeight);
                                textGraphics.DrawLine(pen, textX - baseWidth / 2f, textY, textX + baseWidth / 2f, textY);
                            }
                        }

                        var bounds = description.TrimTransparency ? BitmapHelper.FindTransparencyBounds(bitmap) : Rectangle.Empty;
                        if (bounds != Rectangle.Empty && bounds != new Rectangle(0, 0, bitmap.Width, bitmap.Height))
                        {
                            using (var trimmedBitmap = new Bitmap(bounds.Width, bounds.Height))
                            {
                                offsetX += bounds.Left;
                                offsetY += bounds.Top;
                                width = trimmedBitmap.Width;
                                height = trimmedBitmap.Height;
                                using (var trimGraphics = Graphics.FromImage(trimmedBitmap)) trimGraphics.DrawImage(bitmap, 0, 0, bounds, GraphicsUnit.Pixel);
                                Misc.WithRetries(() => trimmedBitmap.Save(bitmapPath, ImageFormat.Png));
                            }
                        }
                        else Misc.WithRetries(() => bitmap.Save(bitmapPath, ImageFormat.Png));

                        if (File.Exists(bitmapPath)) BitmapHelper.Compress(bitmapPath, optimize);
                    }
                }
            }
            return new FontTexture(Path.Combine(Directory, filename), offsetX, offsetY, baseWidth, baseHeight, width, height);
        }
        internal void HandleCache(TinyToken cachedFontRoot)
        {
            if (!matches(cachedFontRoot)) return;

            foreach (var cacheEntry in cachedFontRoot.Values<TinyObject>("Cache"))
            {
                var path = cacheEntry.Value<string>("Path");
                var hash = cacheEntry.Value<string>("Hash");

                var fullPath = Path.Combine(assetDirectory, path);
                if (!File.Exists(fullPath) || HashHelper.GetFileMd5(fullPath) != hash) continue;

                var text = cacheEntry.Value<string>("Text");
                if (text.Contains('\ufffd'))
                {
                    Trace.WriteLine($"Ignoring invalid font texture \"{text}\" ({path})");
                    continue;
                }
                if (textureCache.ContainsKey(text)) throw new InvalidDataException($"The font texture for \"{text}\" ({path}) has been cached multiple times");

                textureCache.Add(text, new FontTexture(path,
                    cacheEntry.Value<float>("OffsetX"), cacheEntry.Value<float>("OffsetY"),
                    cacheEntry.Value<int>("BaseWidth"), cacheEntry.Value<int>("BaseHeight"),
                    cacheEntry.Value<int>("Width"), cacheEntry.Value<int>("Height")
                ));
            }
        }
        bool matches(TinyToken cachedFontRoot)
        {
            if (cachedFontRoot.Value<string>("FontPath") == description.FontPath &&
                cachedFontRoot.Value<int>("FontSize") == description.FontSize &&
                MathUtil.FloatEquals(cachedFontRoot.Value<float>("ColorR"), description.Color.R, .00001f) &&
                MathUtil.FloatEquals(cachedFontRoot.Value<float>("ColorG"), description.Color.G, .00001f) &&
                MathUtil.FloatEquals(cachedFontRoot.Value<float>("ColorB"), description.Color.B, .00001f) &&
                MathUtil.FloatEquals(cachedFontRoot.Value<float>("ColorA"), description.Color.A, .00001f) &&
                MathUtil.FloatEquals(cachedFontRoot.Value<float>("PaddingX"), description.Padding.X, .00001f) &&
                MathUtil.FloatEquals(cachedFontRoot.Value<float>("PaddingY"), description.Padding.Y, .00001f) &&
                cachedFontRoot.Value<FontStyle>("FontStyle") == description.FontStyle &&
                cachedFontRoot.Value<bool>("TrimTransparency") == description.TrimTransparency &&
                cachedFontRoot.Value<bool>("EffectsOnly") == description.EffectsOnly &&
                cachedFontRoot.Value<bool>("Debug") == description.Debug)
            {
                var effectsRoot = cachedFontRoot.Value<TinyArray>("Effects");
                if (effectsRoot.Count != effects.Length) return false;

                for (var i = 0; i < effects.Length; i++) if (!matches(effects[i], effectsRoot[i].Value<TinyToken>()))
                    return false;

                return true;
            }
            return false;
        }
        bool matches(FontEffect fontEffect, TinyToken cache)
        {
            var effectType = fontEffect.GetType();
            if (cache.Value<string>("Type") != effectType.FullName) return false;

            foreach (var field in effectType.GetFields())
            {
                var fieldType = field.FieldType;
                if (fieldType == typeof(Color4))
                {
                    var color = (Color4)field.GetValue(fontEffect);
                    if (!MathUtil.FloatEquals(cache.Value<float>($"{field.Name}R"), color.R, .00001f) ||
                        !MathUtil.FloatEquals(cache.Value<float>($"{field.Name}G"), color.G, .00001f) ||
                        !MathUtil.FloatEquals(cache.Value<float>($"{field.Name}B"), color.B, .00001f) ||
                        !MathUtil.FloatEquals(cache.Value<float>($"{field.Name}A"), color.A, .00001f))
                        return false;
                }
                else if (fieldType == typeof(Vector3))
                {
                    var vector = (Vector3)field.GetValue(fontEffect);
                    if (!MathUtil.FloatEquals(cache.Value<float>($"{field.Name}X"), vector.X, .00001f) ||
                        !MathUtil.FloatEquals(cache.Value<float>($"{field.Name}Y"), vector.Y, .00001f) ||
                        !MathUtil.FloatEquals(cache.Value<float>($"{field.Name}Z"), vector.Z, .00001f))
                        return false;
                }
                else if (fieldType == typeof(Vector2))
                {
                    var vector = (Vector2)field.GetValue(fontEffect);
                    if (!MathUtil.FloatEquals(cache.Value<float>($"{field.Name}X"), vector.X, .00001f) ||
                        !MathUtil.FloatEquals(cache.Value<float>($"{field.Name}Y"), vector.Y, .00001f))
                        return false;
                }
                else if (fieldType == typeof(double))
                {
                    if (!MathUtil.DoubleEquals(cache.Value<double>(field.Name), (double)field.GetValue(fontEffect), .00001))
                        return false;
                }
                else if (fieldType == typeof(float))
                {
                    if (!MathUtil.FloatEquals(cache.Value<float>(field.Name), (float)field.GetValue(fontEffect), .00001f))
                        return false;
                }
                else if (fieldType == typeof(int) || fieldType.IsEnum)
                {
                    if (cache.Value<int>(field.Name) != (int)field.GetValue(fontEffect)) return false;
                }
                else if (fieldType == typeof(string))
                {
                    if (cache.Value<string>(field.Name) != (string)field.GetValue(fontEffect)) return false;
                }
                else throw new InvalidDataException($"Unexpected field type {fieldType} for {field.Name} in {effectType.FullName}");
            }
            return true;
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
            { "Effects", effects.Select(e => fontEffectToTinyObject(e))},
            { "Cache", textureCache.Where(l => !l.Value.IsEmpty).Select(l => letterToTinyObject(l))}
        };
        TinyObject letterToTinyObject(KeyValuePair<string, FontTexture> letterEntry) => new TinyObject
        {
            { "Text", letterEntry.Key },
            { "Path", PathHelper.WithStandardSeparators(letterEntry.Value.Path) },
            { "Hash", HashHelper.GetFileMd5(Path.Combine(assetDirectory, letterEntry.Value.Path)) },
            { "OffsetX", letterEntry.Value.OffsetX },
            { "OffsetY", letterEntry.Value.OffsetY },
            { "BaseWidth", letterEntry.Value.BaseWidth },
            { "BaseHeight", letterEntry.Value.BaseHeight },
            { "Width", letterEntry.Value.Width },
            { "Height", letterEntry.Value.Height }
        };
        TinyObject fontEffectToTinyObject(FontEffect fontEffect)
        {
            var effectType = fontEffect.GetType();
            var cache = new TinyObject
            {
                ["Type"] = effectType.FullName
            };

            foreach (var field in effectType.GetFields())
            {
                var fieldType = field.FieldType;
                if (fieldType == typeof(Color4))
                {
                    var color = (Color4)field.GetValue(fontEffect);
                    cache[$"{field.Name}R"] = color.R;
                    cache[$"{field.Name}G"] = color.G;
                    cache[$"{field.Name}B"] = color.B;
                    cache[$"{field.Name}A"] = color.A;
                }
                else if (fieldType == typeof(Vector3))
                {
                    var vector = (Vector3)field.GetValue(fontEffect);
                    cache[$"{field.Name}X"] = vector.X;
                    cache[$"{field.Name}Y"] = vector.Y;
                    cache[$"{field.Name}Z"] = vector.Z;
                }
                else if (fieldType == typeof(Vector2))
                {
                    var vector = (Vector2)field.GetValue(fontEffect);
                    cache[$"{field.Name}X"] = vector.X;
                    cache[$"{field.Name}Y"] = vector.Y;
                }
                else if (fieldType == typeof(double)) cache[field.Name] = (double)field.GetValue(fontEffect);
                else if (fieldType == typeof(float)) cache[field.Name] = (float)field.GetValue(fontEffect);
                else if (fieldType == typeof(int) || fieldType.IsEnum) cache[field.Name] = (int)field.GetValue(fontEffect);
                else if (fieldType == typeof(string)) cache[field.Name] = (string)field.GetValue(fontEffect);
                else throw new InvalidDataException($"Unexpected field type {fieldType} for {field.Name} in {effectType.FullName}");
            }
            return cache;
        }
    }
}