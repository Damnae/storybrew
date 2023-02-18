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
        TextLayout textLayout;

        TextFont font;
        float currentFontSize;
        float currentScaling = 1;

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

        string text = "";
        public string Text
        {
            get => text;
            set
            {
                if (text == value) return;
                text = value;
                invalidate();
            }
        }

        public IconFont Icon { get => text.Length == 1 ? (IconFont)text[0] : 0; set => Text = ((char)value).ToString(); }

        string fontName = "Tahoma";
        public string FontName
        {
            get => fontName;
            set
            {
                if (fontName == value) return;
                fontName = value;
                invalidate();
            }
        }

        float fontSize = 12;
        public float FontSize
        {
            get => fontSize;
            set
            {
                if (fontSize == value) return;
                fontSize = value;
                invalidate();
            }
        }

        Vector2 maxSize;
        public Vector2 MaxSize
        {
            get => maxSize;
            set
            {
                if (maxSize == value) return;
                maxSize = value;
                invalidate();
            }
        }

        float scaling = 1;
        public float Scaling
        {
            get => scaling;
            set
            {
                if (scaling == value) return;
                scaling = value;
                invalidate();
            }
        }

        BoxAlignment alignment = BoxAlignment.TopLeft;
        public BoxAlignment Alignment
        {
            get => alignment;
            set
            {
                if (alignment == value) return;
                alignment = value;
                invalidate();
            }
        }

        StringTrimming trimming = StringTrimming.None;
        public StringTrimming Trimming
        {
            get => trimming;
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

            var renderer = DrawState.Prepare(drawContext.Get<QuadRenderer>(), camera, RenderStates);

            var clipRegion = DrawState.GetClipRegion(camera) ?? Box2.FromDimensions(
                new Vector2(camera.ExtendedViewport.Left, camera.ExtendedViewport.Top) + camera.Position.Xy,
                new Vector2(camera.ExtendedViewport.Width, camera.ExtendedViewport.Height));

            foreach (var layoutGlyph in textLayout.VisibleGlyphs)
            {
                var glyph = layoutGlyph.Glyph;
                var position = layoutGlyph.Position;

                var y = bounds.Top + position.Y * inverseScaling;
                var height = glyph.Height * inverseScaling;

                if (y > clipRegion.Bottom) break;
                if (y + height < clipRegion.Top) continue;

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
            textLayout.ForTextBounds(startIndex, endIndex, bounds => action(new Box2(
                bounds.Left * inverseScaling, bounds.Top * inverseScaling, bounds.Right * inverseScaling, bounds.Bottom * inverseScaling)));
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

        void invalidate() => textLayout = null;
        void validate()
        {
            if (textLayout != null) return;
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

        protected virtual void Dispose(bool disposing)
        {
            if (disposing) font?.Dispose();
            font = null;
        }
        public void Dispose() => Dispose(true);

        #endregion
    }
}