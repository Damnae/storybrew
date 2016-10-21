using OpenTK;
using OpenTK.Graphics;
using StorybrewEditor.Graphics.Cameras;
using StorybrewEditor.Graphics.Text;
using StorybrewEditor.UserInterface;
using StorybrewEditor.Util;
using System;
using System.Drawing;

namespace StorybrewEditor.Graphics.Drawables
{
    public class TextDrawable : Drawable
    {
        private TextLayout textLayout;

        private TextFont font;
        private float textureFontSize;
        private float textureScaling = 1;

        public Vector2 MinSize => Size;
        public Vector2 PreferredSize => Size;

        public Vector2 Size
        {
            get
            {
                validate();
                return textLayout.Size;
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

        private UiAlignment alignment = UiAlignment.TopLeft;
        public UiAlignment Alignment
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
            drawText(drawContext, camera, bounds, opacity);
        }

        private void drawText(DrawContext drawContext, Camera camera, Box2 bounds, float opacity)
        {
            var inverseScaling = 1 / Scaling;
            var color = Color.WithOpacity(opacity);

            var renderer = DrawState.Prepare(drawContext.SpriteRenderer, camera, RenderStates);
            var clipRegion = DrawState.GetClipRegion(camera) ?? new Box2(camera.ExtendedViewport.Left, camera.ExtendedViewport.Top, camera.ExtendedViewport.Right, camera.ExtendedViewport.Bottom);

            foreach (var layoutLine in textLayout.Lines)
                foreach (var layoutGlyph in layoutLine.Glyphs)
                {
                    var glyph = layoutGlyph.Glyph;
                    if (glyph.IsEmpty)
                        continue;

                    var position = layoutGlyph.Position;

                    var x = bounds.Left + position.X * inverseScaling;
                    var y = bounds.Top + position.Y * inverseScaling;
                    var width = glyph.Width * inverseScaling;
                    var height = glyph.Height * inverseScaling;

                    if (y <= clipRegion.Bottom && y + height >= clipRegion.Top)
                        renderer.Draw(glyph.Texture, x, y, 0, 0, inverseScaling, inverseScaling, 0, color);
                }
        }

        private void invalidate()
        {
            textLayout = null;
        }

        private void validate()
        {
            if (textLayout != null)
                return;

            if (font == null || font.Name != FontName || textureFontSize != FontSize || textureScaling != Scaling)
            {
                font?.Dispose();
                font = DrawState.TextFontManager.GetTextFont(FontName, FontSize, Scaling);

                textureFontSize = FontSize;
                textureScaling = Scaling;
            }

            var text = Text;
            if (string.IsNullOrEmpty(text)) text = " ";
            if (text.EndsWith("\n")) text += " ";
            textLayout = new TextLayout(text, font, alignment, trimming, (int)Math.Ceiling(MaxSize.X * Scaling));
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
