using OpenTK;
using StorybrewEditor.Graphics.Cameras;
using StorybrewEditor.Graphics.Text;
using StorybrewEditor.Util;
using System;
using System.Collections.Generic;

namespace StorybrewEditor.Graphics.Drawables
{
    public class TextMultiDrawable : TextDrawable
    {
        private List<string> lines;

        private TextFont font;
        private Vector2 textureMaxSize;
        private float textureFontSize;
        private float textureScaling = 1;
        private Vector2 measuredSize;

        protected override Vector2 TextureMaxSize => textureMaxSize;
        protected override float TextureScaling => textureScaling;

        public override Vector2 Size
        {
            get
            {
                ValidateMeasuredSize();
                return measuredSize;
            }
        }

        protected override void DrawText(DrawContext drawContext, Camera camera, Box2 bounds, float opacity)
        {
            var inverseScaling = 1 / Scaling;
            var color = Color.WithOpacity(opacity);

            var renderer = DrawState.Prepare(drawContext.SpriteRenderer, camera, RenderStates);

            var y = bounds.Top;
            var lineHasNonSpacing = true;
            foreach (var line in lines)
            {
                var x = bounds.Left;
                var lineHeight = 0f;
                foreach (var c in line)
                {
                    var character = font.GetCharacter(c);
                    if (!character.IsEmpty)
                    {
                        renderer.Draw(character.Texture, x, y, 0, 0, inverseScaling, inverseScaling, 0, color);
                        lineHasNonSpacing = true;
                    }

                    if (lineHasNonSpacing)
                        x += character.Width * inverseScaling;
                    lineHeight = Math.Max(lineHeight, character.Height * inverseScaling);
                }
                lineHasNonSpacing = false;
                y += lineHeight;

                if (y >= bounds.Bottom)
                    break;
            }
        }

        protected override void ValidateMeasuredSize() => validate();
        protected override void InvalidateMeasuredSize() => invalidate();
        protected override void InvalidateTexture() => invalidate();
        protected override void ValidateTexture() => validate();

        private void invalidate()
        {
            lines = null;
        }

        private void validate()
        {
            if (lines != null) return;

            updateFont();
            splitLines();
            measureLines();
        }

        private void updateFont()
        {
            if (font == null || font.Name != FontName || textureFontSize != FontSize || textureScaling != Scaling)
            {
                font?.Dispose();
                font = DrawState.TextFontManager.GetTextFont(FontName, FontSize, Scaling);
            }
            textureFontSize = FontSize;
            textureScaling = Scaling;
        }

        private void splitLines()
        {
            var text = Text;
            if (string.IsNullOrEmpty(text)) text = " ";
            if (text.EndsWith("\n")) text += " ";

            lines = LineBreaker.Split(text, (float)Math.Ceiling(MaxSize.X * Scaling), c => font.GetCharacter(c).Width);
            textureMaxSize = MaxSize;
        }

        private void measureLines()
        {
            var width = 0.0f;
            var height = 0.0f;
            foreach (var line in lines)
            {
                var lineWidth = 0f;
                var lineHeight = 0f;
                foreach (var c in line)
                {
                    var character = font.GetCharacter(c);
                    lineWidth += character.Width;
                    lineHeight = Math.Max(lineHeight, character.Height);
                }
                width = Math.Max(width, lineWidth);
                height += lineHeight;
            }
            measuredSize = new Vector2(width, height) / Scaling;
        }

        #region IDisposable Support

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (disposing)
            {
                font?.Dispose();
            }
            font = null;
        }

        #endregion
    }
}
