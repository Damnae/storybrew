using System;

namespace StorybrewEditor.Graphics.Text
{
    public class TextFontProxy : TextFont
    {
        private TextFont textFont;
        private Action disposed;
        
        public TextFontProxy(TextFont textFont, Action disposed) : base(textFont.Name, textFont.Size)
        {
            this.textFont = textFont;
            this.disposed = disposed;
        }

        public override Character GetCharacter(char c)
            => textFont.GetCharacter(c);
        
        #region IDisposable Support

        private bool disposedValue = false;
        protected override void Dispose(bool disposing)
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

        #endregion
    }
}
