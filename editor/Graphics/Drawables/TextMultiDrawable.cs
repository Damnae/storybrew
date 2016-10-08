using OpenTK;
using StorybrewEditor.Graphics.Cameras;
using StorybrewEditor.Graphics.Text;
using StorybrewEditor.Util;
using System;
using System.Collections.Generic;
using System.Text;

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
            foreach (var line in lines)
            {
                var x = bounds.Left;
                var lineHeight = 0f;
                foreach (var c in line)
                {
                    var character = font.GetCharacter(c);
                    if (!character.IsEmpty) renderer.Draw(character.Texture, x, y, 0, 0, inverseScaling, inverseScaling, 0, color);

                    x += character.BaseWidth * inverseScaling;
                    lineHeight = Math.Max(lineHeight, character.BaseHeight * inverseScaling);
                }
                y += lineHeight;
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
            lines = new List<string>();

            if (font == null || font.Name != FontName || textureFontSize != FontSize || textureScaling != Scaling)
            {
                font?.Dispose();
                font = new TextFont(FontName, FontSize * Scaling);
            }
            textureMaxSize = MaxSize;
            textureFontSize = FontSize;
            textureScaling = Scaling;

            var width = 0.0f;
            var height = 0.0f;

            var text = Text;
            if (string.IsNullOrEmpty(text)) text = " ";
            if (text.EndsWith("\n")) text += " ";

            var sb = new StringBuilder();
            var lineWidth = 0;
            var lineHeight = 0;

            Action completeLine = () =>
            {
                width = Math.Max(width, lineWidth);
                height += lineHeight;

                lineWidth = 0;
                lineHeight = 0;

                lines.Add(sb.ToString());
                sb.Clear();
            };

            for (var i = 0; i < text.Length; i++)
            {
                var c = text[i];
                var character = font.GetCharacter(c);

                if (MaxSize.X > 0 && (lineWidth + character.BaseWidth) / Scaling > MaxSize.X)
                    completeLine();

                lineWidth += character.BaseWidth;
                lineHeight = Math.Max(lineHeight, character.BaseHeight);

                sb.Append(c);
            }
            if (sb.Length > 0)
                completeLine();

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
