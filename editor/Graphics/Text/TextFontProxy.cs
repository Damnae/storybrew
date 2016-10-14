using System;

namespace StorybrewEditor.Graphics.Text
{
    public class TextFontProxy : TextFont
    {
        private TextFont textFont;
        private Action disposed;

        public string Name => textFont.Name;
        public float Size => textFont.Size;

        public TextFontProxy(TextFont textFont, Action disposed)
        {
            this.textFont = textFont;
            this.disposed = disposed;
        }

        public FontCharacter GetCharacter(char c)
            => textFont.GetCharacter(c);

        #region IDisposable Support

        private bool disposedValue = false;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    disposed();
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
