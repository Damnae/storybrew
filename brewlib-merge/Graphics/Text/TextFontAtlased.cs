using BrewLib.Graphics.Textures;
using BrewLib.Util;
using OpenTK;
using System.Collections.Generic;
using System.Drawing;

namespace BrewLib.Graphics.Text
{
    public class TextFontAtlased : TextFont
    {
        Dictionary<char, FontGlyph> glyphs = new Dictionary<char, FontGlyph>();
        TextureMultiAtlas2d atlas;
        readonly string name;
        public string Name => name;

        readonly float size;
        public float Size => size;
        public int LineHeight => GetGlyph(' ').Height;

        public TextFontAtlased(string name, float size)
        {
            this.name = name;
            this.size = size;
        }

        public FontGlyph GetGlyph(char c)
        {
            if (!glyphs.TryGetValue(c, out FontGlyph glyph)) glyphs.Add(c, glyph = generateGlyph(c));
            return glyph;
        }
        FontGlyph generateGlyph(char c)
        {
            Vector2 measuredSize;
            if (char.IsWhiteSpace(c))
            {
                DrawState.TextGenerator.CreateBitmap(c.ToString(), name, size,
                    Vector2.Zero, Vector2.Zero, BoxAlignment.Centre, StringTrimming.None, out measuredSize, true);

                return new FontGlyph(null, (int)measuredSize.X, (int)measuredSize.Y);
            }
            else
            {
                atlas = atlas ?? new TextureMultiAtlas2d(512, 512, $"Font Atlas {name} {size}x");
                using (var bitmap = DrawState.TextGenerator.CreateBitmap(c.ToString(), name, size,
                    Vector2.Zero, Vector2.Zero, BoxAlignment.Centre, StringTrimming.None, out measuredSize, false))
                {
                    var texture = atlas.AddRegion(bitmap, $"glyph:{c}@{Name}:{Size}");
                    return new FontGlyph(texture, (int)measuredSize.X, (int)measuredSize.Y);
                }
            }
        }

        #region IDisposable Support

        bool disposedValue = false;
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    foreach (var glyph in glyphs.Values) glyph.Texture?.Dispose();
                    atlas?.Dispose();
                }
                glyphs = null;
                atlas = null;
                disposedValue = true;
            }
        }
        public void Dispose() => Dispose(true);

        #endregion
    }
}