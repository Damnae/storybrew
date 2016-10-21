using OpenTK;
using StorybrewEditor.Graphics.Textures;
using StorybrewEditor.UserInterface;
using System.Collections.Generic;
using System.Drawing;

namespace StorybrewEditor.Graphics.Text
{
    public class TextFontAtlased : TextFont
    {
        private Dictionary<char, FontGlyph> glyphs = new Dictionary<char, FontGlyph>();
        private TextureMultiAtlas2d atlas;

        private string name;
        public string Name => name;

        private float size;
        public float Size => size;

        public TextFontAtlased(string name, float size)
        {
            this.name = name;
            this.size = size;
        }

        public FontGlyph GetGlyph(char c)
        {
            FontGlyph glyph;
            if (!glyphs.TryGetValue(c, out glyph))
                glyphs.Add(c, glyph = generateGlyph(c));
            return glyph;
        }

        private FontGlyph generateGlyph(char c)
        {
            Vector2 measuredSize;
            if (char.IsWhiteSpace(c))
            {
                DrawState.TextGenerator.CreateBitmap(c.ToString(), name, size,
                    Vector2.Zero, Vector2.Zero, UiAlignment.Centre, StringTrimming.None, out measuredSize, true);
                return new FontGlyph(null, (int)measuredSize.X, (int)measuredSize.Y);
            }
            else
            {
                atlas = atlas ?? new TextureMultiAtlas2d(512, 512, $"Font Atlas {name} {size}x");
                using (var bitmap = DrawState.TextGenerator.CreateBitmap(c.ToString(), name, size,
                    Vector2.Zero, Vector2.Zero, UiAlignment.Centre, StringTrimming.None, out measuredSize, false))
                {
                    var texture = atlas.AddRegion(bitmap, $"glyph:{c}@{Name}:{Size}");
                    return new FontGlyph(texture, (int)measuredSize.X, (int)measuredSize.Y);
                }
            }
        }

        #region IDisposable Support

        private bool disposedValue = false;
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    foreach (var glyph in glyphs.Values)
                        glyph.Texture?.Dispose();
                    atlas?.Dispose();
                }
                glyphs = null;
                atlas = null;
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
