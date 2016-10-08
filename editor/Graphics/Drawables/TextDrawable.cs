using OpenTK;
using OpenTK.Graphics;
using StorybrewEditor.Graphics.Cameras;
using StorybrewEditor.UserInterface;
using StorybrewEditor.Util;
using System.Drawing;

namespace StorybrewEditor.Graphics.Drawables
{
    public abstract class TextDrawable : Drawable
    {
        public Vector2 MinSize => Size;
        public Vector2 PreferredSize => Size;

        public abstract Vector2 Size { get; }

        private string text = string.Empty;
        public string Text
        {
            get { return text; }
            set
            {
                if (text == value) return;
                text = value;
                InvalidateTexture();
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
                InvalidateTexture();
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
                InvalidateTexture();
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
                InvalidateMeasuredSize();
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
                InvalidateMeasuredSize();
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
                InvalidateTexture();
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
                InvalidateTexture();
            }
        }

        public readonly RenderStates RenderStates = new RenderStates();
        public Color4 Color = Color4.White;

        public void Draw(DrawContext drawContext, Camera camera, Box2 bounds, float opacity)
        {
            if (TextureMaxSize != maxSize || TextureScaling != scaling) InvalidateTexture();
            ValidateTexture();
            DrawText(drawContext, camera, bounds, opacity);
        }

        protected abstract Vector2 TextureMaxSize { get; }
        protected abstract float TextureScaling { get; }

        protected abstract void DrawText(DrawContext drawContext, Camera camera, Box2 bounds, float opacity);
        protected abstract void ValidateMeasuredSize();
        protected abstract void InvalidateMeasuredSize();
        protected abstract void ValidateTexture();
        protected abstract void InvalidateTexture();

        #region IDisposable Support

        private bool disposedValue = false;
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                }
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
