using System;

namespace BrewLib.Graphics.Text
{
    public class TextFontProxy : TextFont
    {
        TextFont textFont;
        readonly Action disposed;

        public string Name => textFont.Name;
        public float Size => textFont.Size;
        public int LineHeight => textFont.LineHeight;

        public TextFontProxy(TextFont textFont, Action disposed)
        {
            this.textFont = textFont;
            this.disposed = disposed;
        }

        public FontGlyph GetGlyph(char c) => textFont.GetGlyph(c);

        #region IDisposable Support

        bool disposedValue = false;
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing) disposed();
                textFont = null;
                disposedValue = true;
            }
        }
        public void Dispose() => Dispose(true);

        #endregion
    }
}