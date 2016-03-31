using OpenTK;
using OpenTK.Graphics;
using StorybrewEditor.Graphics;
using StorybrewEditor.Graphics.Cameras;
using StorybrewEditor.Graphics.Textures;
using StorybrewEditor.Util;
using System.Drawing;

namespace StorybrewEditor.UserInterface.Drawables
{
    public class TextDrawable : Drawable
    {
        private Texture2d texture;
        private Vector2 textureMaxSize;
        private float textureScaling = 1;
        private Vector2 measuredSize;
        private bool textureMeasured;

        public Vector2 MinSize => Size;
        public Vector2 PreferredSize => Size;

        public Vector2 Size
        {
            get
            {
                validateMeasuredSize();
                return measuredSize;
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
                invalidateTexture();
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
                invalidateTexture();
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
                invalidateTexture();
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

                // Since the max size is likely to go back to the value used at the previous draw call,
                // the texture isn't invalidated now, but right before drawing.
                invalidateMeasuredSize();
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
                invalidateMeasuredSize();
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
                invalidateTexture();
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
                invalidateTexture();
            }
        }

        public BlendingMode BlendingMode = BlendingMode.Default;
        public Color4 Color = Color4.White;

        public void Draw(DrawContext drawContext, Camera camera, Box2 bounds, float opacity)
        {
            if (textureMaxSize != maxSize || textureScaling != scaling) invalidateTexture();
            validateTexture();

            var renderer = drawContext.SpriteRenderer;
            DrawState.Renderer = renderer;
            DrawState.SetBlending(BlendingMode);
            renderer.Camera = camera;
            renderer.Draw(texture, bounds.Left, bounds.Top, 0, 0, 1 / scaling, 1 / scaling, 0, Color.WithOpacity(opacity));
        }

        private void validateMeasuredSize()
        {
            if (textureMeasured) return;
            textureMeasured = true;

            DrawState.FontManager.CreateBitmap(text, fontName, fontSize * scaling, maxSize * scaling, Vector2.Zero, alignment, trimming, out measuredSize, true);
            measuredSize /= scaling;
        }

        private void invalidateMeasuredSize()
        {
            textureMeasured = false;
        }

        private void validateTexture()
        {
            if (texture != null) return;
            textureMeasured = true;
            textureMaxSize = maxSize;
            textureScaling = scaling;

            texture = DrawState.FontManager.CreateTexture(text, fontName, fontSize * scaling, maxSize * scaling, Vector2.Zero, alignment, trimming, out measuredSize);
            measuredSize /= scaling;
        }

        private void invalidateTexture()
        {
            texture?.Dispose();
            texture = null;
            textureMaxSize = Vector2.Zero;
            invalidateMeasuredSize();
        }

        #region IDisposable Support

        private bool disposedValue = false;
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    texture?.Dispose();
                }
                texture = null;
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
