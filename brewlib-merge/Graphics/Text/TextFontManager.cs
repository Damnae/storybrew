using System;
using System.Collections.Generic;

namespace BrewLib.Graphics.Text
{
    public class TextFontManager : IDisposable
    {
        Dictionary<string, TextFont> fonts = new Dictionary<string, TextFont>();
        readonly Dictionary<string, int> references = new Dictionary<string, int>();

        public TextFont GetTextFont(string fontName, float fontSize, float scaling)
        {
            var identifier = $"{fontName}|{fontSize}|{scaling}";

            if (!fonts.TryGetValue(identifier, out TextFont font)) fonts.Add(identifier, font = new TextFontAtlased(fontName, fontSize * scaling));
            if (references.TryGetValue(identifier, out int refCount)) references[identifier] = refCount + 1;
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

        bool disposedValue = false;
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing) foreach (var entry in fonts) entry.Value.Dispose();
                fonts = null;
                disposedValue = true;
            }
        }
        public void Dispose() => Dispose(true);

        #endregion
    }
}