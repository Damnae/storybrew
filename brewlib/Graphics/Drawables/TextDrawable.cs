using BrewLib.Graphics.Cameras;
using BrewLib.Graphics.Renderers;
using BrewLib.Graphics.Text;
using BrewLib.Util;
using OpenTK;
using OpenTK.Graphics;
using System;
using System.Drawing;

namespace BrewLib.Graphics.Drawables
{
    public class TextDrawable : Drawable
    {
        private TextLayout textLayout;

        private TextFont font;
        private float currentFontSize;
        private float currentScaling = 1;

        public Vector2 MinSize => Size;
        public Vector2 PreferredSize => Size;

        public Vector2 Size
        {
            get
            {
                validate();
                return text?.Length > 0 ? textLayout.Size / scaling : font.GetGlyph(' ').Size / scaling;
            }
        }

        private string text = string.Empty;
        public string Text
        {
            get { return text; }
            set
            {
                if (text == value) return;
                text = value;
                invalidate();
            }
        }

        public IconFont Icon { get { return text.Length == 1 ? (IconFont)text[0] : 0; } set { Text = ((char)value).ToString(); } }

        private string fontName = "Tahoma";
        public string FontName
        {
            get { return fontName; }
            set
            {
                if (fontName == value) return;
                fontName = value;
                invalidate();
            }
        }

        private float fontSize = 12;
        public float FontSize
        {
            get { return fontSize; }
            set
            {
                if (fontSize == value) return;
                fontSize = value;
                invalidate();
            }
        }

        private Vector2 maxSize;
        public Vector2 MaxSize
        {
            get { return maxSize; }
            set
            {
                if (maxSize == value) return;
                maxSize = value;
                invalidate();
            }
        }

        private float scaling = 1;
        public float Scaling
        {
            get { return scaling; }
            set
            {
                if (scaling == value) return;
                scaling = value;
                invalidate();
            }
        }

        private BoxAlignment alignment = BoxAlignment.TopLeft;
        public BoxAlignment Alignment
        {
            get { return alignment; }
            set
            {
                if (alignment == value) return;
                alignment = value;
                invalidate();
            }
        }

        private StringTrimming trimming = StringTrimming.None;
        public StringTrimming Trimming
        {
            get { return trimming; }
            set
            {
                if (trimming == value) return;
                trimming = value;
                invalidate();
            }
        }

        public readonly RenderStates RenderStates = new RenderStates();
        public Color4 Color = Color4.White;

        public void Draw(DrawContext drawContext, Camera camera, Box2 bounds, float opacity)
        {
            validate();

            var inverseScaling = 1 / scaling;
            var color = Color.WithOpacity(opacity);

            var renderer = DrawState.Prepare(drawContext.Get<SpriteRenderer>(), camera, RenderStates);
            var clipRegion = DrawState.GetClipRegion(camera) ?? new Box2(camera.ExtendedViewport.Left, camera.ExtendedViewport.Top, camera.ExtendedViewport.Right, camera.ExtendedViewport.Bottom);

            foreach (var layoutGlyph in textLayout.VisibleGlyphs)
            {
                var glyph = layoutGlyph.Glyph;
                var position = layoutGlyph.Position;

                var y = bounds.Top + position.Y * inverseScaling;
                var height = glyph.Height * inverseScaling;

                if (y > clipRegion.Bottom)// || y - height > bounds.Bottom)
                    break;

                if (y + height < clipRegion.Top)// || y < bounds.Top)
                    continue;

                var x = bounds.Left + position.X * inverseScaling;
                var width = glyph.Width * inverseScaling;

                renderer.Draw(glyph.Texture, x, y, 0, 0, inverseScaling, inverseScaling, 0, color);
            }
        }

        public Box2 GetCharacterBounds(int index)
        {
            validate();

            var inverseScaling = 1 / scaling;
            var layoutGlyph = textLayout.GetGlyph(index);
            var glyph = layoutGlyph.Glyph;
            var position = layoutGlyph.Position * inverseScaling;

            return new Box2(position.X, position.Y, position.X + glyph.Width * inverseScaling, position.Y + glyph.Height * inverseScaling);
        }

        public int GetCharacterIndexAt(Vector2 position)
        {
            validate();
            return textLayout.GetCharacterIndexAt(position * scaling);
        }

        public void ForTextBounds(int startIndex, int endIndex, Action<Box2> action)
        {
            validate();
            var inverseScaling = 1 / scaling;
            textLayout.ForTextBounds(startIndex, endIndex, bounds =>
                action(new Box2(bounds.Left * inverseScaling, bounds.Top * inverseScaling, bounds.Right * inverseScaling, bounds.Bottom * inverseScaling)));
        }

        public int GetCharacterIndexAbove(int index)
        {
            validate();
            return textLayout.GetCharacterIndexAbove(index);
        }

        public int GetCharacterIndexBelow(int index)
        {
            validate();
            return textLayout.GetCharacterIndexBelow(index);
        }

        private void invalidate()
        {
            textLayout = null;
        }

        private void validate()
        {
            if (textLayout != null)
                return;

            if (font == null || font.Name != FontName || currentFontSize != FontSize || currentScaling != Scaling)
            {
                font?.Dispose();
                font = DrawState.TextFontManager.GetTextFont(FontName, FontSize, Scaling);

                currentFontSize = FontSize;
                currentScaling = Scaling;
            }

            textLayout = new TextLayout(Text ?? "", font, alignment, trimming, MaxSize * scaling);
        }

        #region IDisposable Support

        private bool disposedValue = false;
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    font?.Dispose();
                }
                font = null;
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
