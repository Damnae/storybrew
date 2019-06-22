using BrewLib.Util;
using OpenTK;
using OpenTK.Graphics;
using StorybrewCommon.Storyboarding;
using System.Collections.Generic;
using System.Drawing;
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
        public FontGeneratorVersion GeneratorVersion = FontGeneratorVersion.Gdi;
    }

    public enum FontGeneratorVersion
    {
        Gdi = 0,
        Media = 1,
    }

    public abstract class FontGenerator
    {
        public string Directory { get; }

        protected readonly FontDescription Description;
        protected readonly FontEffect[] Effects;
        protected readonly string ProjectDirectory;
        protected readonly string MapsetDirectory;

        private Dictionary<string, FontTexture> textureCache = new Dictionary<string, FontTexture>();
        protected int TextureCacheSize => textureCache.Count;

        internal FontGenerator(string directory, FontDescription description, FontEffect[] effects, string projectDirectory, string mapsetDirectory)
        {
            Directory = directory;
            Description = description;
            Effects = effects;
            ProjectDirectory = projectDirectory;
            MapsetDirectory = mapsetDirectory;
        }

        public FontTexture GetTexture(string text)
        {
            if (!textureCache.TryGetValue(text, out FontTexture texture))
            {
                var filename = text.Length == 1 ? $"{(int)text[0]:x4}.png" : $"_{textureCache.Count(l => l.Key.Length > 1):x3}.png";
                var bitmapPath = Path.Combine(Directory, filename);

                var absoluteBitmapPath = Path.Combine(MapsetDirectory, bitmapPath);
                System.IO.Directory.CreateDirectory(Path.GetDirectoryName(absoluteBitmapPath));

                var fontPath = Path.Combine(ProjectDirectory, Description.FontPath);
                if (!File.Exists(fontPath)) fontPath = Description.FontPath;

                textureCache.Add(text, texture = GenerateTexture(text, fontPath, bitmapPath));
            }
            return texture;
        }

        protected abstract FontTexture GenerateTexture(string text, string fontPath, string bitmapPath);

        internal void HandleCache(TinyToken cachedFontRoot)
        {
            if (!matches(cachedFontRoot))
                return;

            foreach (var cacheEntry in cachedFontRoot.Values<TinyObject>("Cache"))
            {
                var path = cacheEntry.Value<string>("Path");
                var hash = cacheEntry.Value<string>("Hash");

                var fullPath = Path.Combine(MapsetDirectory, path);
                if (!File.Exists(fullPath) || HashHelper.GetFileMd5(fullPath) != hash)
                    continue;

                textureCache.Add(cacheEntry.Value<string>("Text"), new FontTexture(
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
            if (cachedFontRoot.Value<string>("FontPath") == Description.FontPath &&
                cachedFontRoot.Value<int>("FontSize") == Description.FontSize &&
                MathUtil.FloatEquals(cachedFontRoot.Value<float>("ColorR"), Description.Color.R, 0.00001f) &&
                MathUtil.FloatEquals(cachedFontRoot.Value<float>("ColorG"), Description.Color.G, 0.00001f) &&
                MathUtil.FloatEquals(cachedFontRoot.Value<float>("ColorB"), Description.Color.B, 0.00001f) &&
                MathUtil.FloatEquals(cachedFontRoot.Value<float>("ColorA"), Description.Color.A, 0.00001f) &&
                MathUtil.FloatEquals(cachedFontRoot.Value<float>("PaddingX"), Description.Padding.X, 0.00001f) &&
                MathUtil.FloatEquals(cachedFontRoot.Value<float>("PaddingY"), Description.Padding.Y, 0.00001f) &&
                cachedFontRoot.Value<FontStyle>("FontStyle") == Description.FontStyle &&
                cachedFontRoot.Value<bool>("TrimTransparency") == Description.TrimTransparency &&
                cachedFontRoot.Value<bool>("EffectsOnly") == Description.EffectsOnly &&
                cachedFontRoot.Value<bool>("Debug") == Description.Debug)
            {
                var effectsRoot = cachedFontRoot.Value<TinyArray>("Effects");
                if (effectsRoot.Count != Effects.Length)
                    return false;

                for (var i = 0; i < Effects.Length; i++)
                    if (!Effects[i].Matches(effectsRoot[i].Value<TinyToken>()))
                        return false;

                return true;
            }
            return false;
        }

        internal TinyObject ToTinyObject() => new TinyObject
        {
            { "FontPath", PathHelper.WithStandardSeparators(Description.FontPath) },
            { "FontSize", Description.FontSize },
            { "ColorR", Description.Color.R },
            { "ColorG", Description.Color.G },
            { "ColorB", Description.Color.B },
            { "ColorA", Description.Color.A },
            { "PaddingX", Description.Padding.X },
            { "PaddingY", Description.Padding.Y },
            { "FontStyle", Description.FontStyle },
            { "TrimTransparency", Description.TrimTransparency },
            { "EffectsOnly", Description.EffectsOnly },
            { "Debug", Description.Debug },
            { "Effects", Effects.Select(e => e.ToTinyObject())},
            { "Cache", textureCache.Where(l => !l.Value.IsEmpty).Select(l => letterToTinyObject(l))},
        };

        private TinyObject letterToTinyObject(KeyValuePair<string, FontTexture> letterEntry) => new TinyObject
        {
            { "Text", letterEntry.Key },
            { "Path", PathHelper.WithStandardSeparators(letterEntry.Value.Path) },
            { "Hash", HashHelper.GetFileMd5(Path.Combine(MapsetDirectory, letterEntry.Value.Path)) },
            { "OffsetX", letterEntry.Value.OffsetX },
            { "OffsetY", letterEntry.Value.OffsetY },
            { "BaseWidth", letterEntry.Value.BaseWidth },
            { "BaseHeight", letterEntry.Value.BaseHeight },
            { "Width", letterEntry.Value.Width },
            { "Height", letterEntry.Value.Height },
        };
    }
}