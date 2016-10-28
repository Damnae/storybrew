using System;
using System.Collections.Generic;

namespace BrewLib.Graphics.Text
{
    public class TextFontManager : IDisposable
    {
        private Dictionary<string, TextFont> fonts = new Dictionary<string, TextFont>();
        private Dictionary<string, int> references = new Dictionary<string, int>();

        public TextFont GetTextFont(string fontName, float fontSize, float scaling)
        {
            var identifier = $"{fontName}|{fontSize}|{scaling}";

            TextFont font;
            if (!fonts.TryGetValue(identifier, out font))
                fonts.Add(identifier, font = new TextFontAtlased(fontName, fontSize * scaling));

            int refCount;
            if (references.TryGetValue(identifier, out refCount))
                references[identifier] = refCount + 1;
            else references[identifier] = 1;

            return new TextFontProxy(font, () =>
            {
                var remaining = --references[identifier];
                if (remaining == 0)
                {
                    fonts.Remove(identifier);
                    font.Dispose();
                }
            });
        }

        #region IDisposable Support

        private bool disposedValue = false;
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    foreach (var entry in fonts)
                        entry.Value.Dispose();
                }
                fonts = null;
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
